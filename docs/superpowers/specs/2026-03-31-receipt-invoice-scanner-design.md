# Receipt & Invoice Scanner — Design Spec

**Date:** 2026-03-31  
**Status:** Approved

---

## Overview

A web application that scans receipt and invoice images/PDFs, extracts structured data using OCR and a local LLM, and returns a downloadable JSON file. No paid cloud services or external APIs — fully local and free.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React + Vite + TypeScript + Tailwind CSS |
| Backend | .NET Core 8 Web API |
| OCR | Tesseract.NET (Tesseract 5, local) |
| LLM | Ollama + LLaVA multimodal model (local) |
| PDF processing | PdfPig (PDF → image conversion) |
| Infrastructure | Docker Compose (3 services) |

---

## Architecture

**Monorepo with Docker Compose.** Three services:

1. `frontend` — React/Vite app (Nginx in production container)
2. `backend` — .NET Core 8 Web API
3. `ollama` — Ollama server with LLaVA model pre-pulled

```
file-scanner/
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   │   ├── UploadZone.tsx
│   │   │   ├── ResultTable.tsx
│   │   │   └── DownloadButton.tsx
│   │   ├── services/
│   │   │   └── scannerApi.ts
│   │   ├── types/
│   │   │   └── ScanResult.ts
│   │   └── App.tsx
│   ├── Dockerfile
│   └── vite.config.ts
├── backend/
│   └── FileScanner.Api/
│       ├── Controllers/
│       │   └── ScanController.cs
│       ├── Services/
│       │   ├── OcrService.cs          ← Tesseract.NET
│       │   ├── LlmService.cs          ← OllamaSharp
│       │   ├── PdfService.cs          ← PdfPig
│       │   └── ExtractionOrchestrator.cs  ← paralel işlem + seçim
│       ├── Models/
│       │   └── ScanResult.cs
│       ├── Dockerfile
│       └── FileScanner.Api.csproj
├── docs/
├── docker-compose.yml
└── CLAUDE.md
```

---

## Data Flow

1. User uploads PNG / JPG / JPEG / WEBP / PDF (multi-page supported)
2. Frontend → `POST /api/scan` (multipart/form-data)
3. Backend receives file
4. If PDF: PdfPig converts each page to image
5. **Parallel processing** for each image:
   - Tesseract.NET extracts raw text → sends to Ollama (llama3 text model) for JSON structuring
   - LLaVA (multimodal) receives the image directly → returns structured JSON
6. `ExtractionOrchestrator` compares both results, selects the one with higher `confidence` score
7. Returns grouped JSON response
8. Frontend renders result table and enables JSON download

---

## JSON Output Schema

```json
{
  "document": {
    "type": "Fatura | Fiş",
    "date": "2024-01-15",
    "invoiceNo": "INV-2024-001"
  },
  "vendor": {
    "name": "Örnek İşletme Ltd.",
    "taxNo": "1234567890",
    "address": "...",
    "phone": "...",
    "email": "..."
  },
  "financials": {
    "subtotal": 950.00,
    "vat": 171.00,
    "total": 1121.00,
    "currency": "TRY",
    "paymentMethod": "Kredi Kartı"
  },
  "items": [
    {
      "name": "Ürün Adı",
      "quantity": 2,
      "unitPrice": 100.00,
      "lineTotal": 200.00
    }
  ],
  "meta": {
    "confidence": 0.95,
    "source": "llm | ocr | combined",
    "processingTimeMs": 1240
  }
}
```

---

## API Endpoints

| Method | Path | Description |
|---|---|---|
| POST | `/api/scan` | Upload file, returns ScanResult JSON |
| GET | `/api/health` | Health check |

**POST /api/scan**
- Content-Type: `multipart/form-data`
- Field: `file` (PNG, JPG, JPEG, WEBP, PDF)
- Max size: 20MB
- Response: `ScanResult` JSON (200) or error (400/500)

---

## Frontend UI

Matches the provided mockup:
- Left panel: drag-and-drop upload zone with thumbnail preview, "Kaldır" button
- Right panel: extracted data table (key-value pairs), "JSON İndir" button top-right
- Language: Turkish UI labels
- Responsive layout (two-column on desktop, stacked on mobile)

---

## Docker Compose Services

```yaml
services:
  frontend:   # React/Vite → Nginx, port 3000
  backend:    # .NET Core API, port 5000
  ollama:     # Ollama server, port 11434, llava model pre-pulled
```

`docker-compose up` starts all three services. Ollama model pulled on first startup via entrypoint script.

---

## Error Handling

- Unsupported file format → 400 with clear message
- OCR and LLM both fail → 500 with error detail
- PDF with no readable content → partial result with low confidence score
- Ollama service unavailable → fallback to OCR-only result

---

## Supported File Formats

- Images: PNG, JPG, JPEG, WEBP
- Documents: PDF (multi-page, each page processed separately)
- Max file size: 20MB

---

## Out of Scope

- User authentication / multi-user support
- Database persistence of scan history
- Batch upload (multiple files at once)
- Paid cloud OCR/LLM services
