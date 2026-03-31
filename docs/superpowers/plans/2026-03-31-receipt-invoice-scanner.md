# Receipt & Invoice Scanner Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a local-only web app that uploads receipt/invoice images or PDFs, extracts structured data via Tesseract OCR + Ollama LLaVA (parallel), and returns a downloadable grouped JSON.

**Architecture:** Monorepo with Docker Compose. React/Vite frontend sends files to a .NET Core 8 API. The API runs Tesseract OCR and Ollama LLaVA in parallel; `ExtractionOrchestrator` picks the result with the higher confidence score and returns a grouped JSON response.

**Tech Stack:** React 18 + Vite + TypeScript + Tailwind CSS | .NET Core 8 Web API | Tesseract.NET | OllamaSharp | PdfPig | Docker Compose

---

## File Map

### Backend — `backend/FileScanner.Api/`
| File | Responsibility |
|---|---|
| `Models/ScanResult.cs` | Shared response model (Document, Vendor, Financials, Items, Meta) |
| `Models/ExtractionCandidate.cs` | Internal model holding a result + confidence score for comparison |
| `Services/PdfService.cs` | Converts PDF pages to byte[] images using PdfPig |
| `Services/OcrService.cs` | Runs Tesseract.NET on image bytes → raw text string |
| `Services/LlmService.cs` | Sends image or text to Ollama → parses JSON response into ScanResult |
| `Services/ExtractionOrchestrator.cs` | Runs OcrService + LlmService in parallel, merges/selects best result |
| `Controllers/ScanController.cs` | POST /api/scan endpoint, GET /api/health |
| `Dockerfile` | .NET 8 multi-stage build |

### Frontend — `frontend/src/`
| File | Responsibility |
|---|---|
| `types/ScanResult.ts` | TypeScript types matching backend ScanResult shape |
| `services/scannerApi.ts` | Axios wrapper for POST /api/scan |
| `components/UploadZone.tsx` | Drag-and-drop / click upload, thumbnail preview, "Kaldır" button |
| `components/ResultTable.tsx` | Renders grouped ScanResult as key-value table |
| `components/DownloadButton.tsx` | Triggers JSON file download |
| `App.tsx` | Two-column layout: UploadZone left, ResultTable + DownloadButton right |

### Infrastructure
| File | Responsibility |
|---|---|
| `docker-compose.yml` | 3 services: frontend (3000), backend (5000), ollama (11434) |
| `backend/FileScanner.Api/Dockerfile` | .NET 8 multi-stage |
| `frontend/Dockerfile` | Node build + Nginx serve |
| `ollama/entrypoint.sh` | Pulls llava model on first start |
| `CLAUDE.md` | Repo guidance |

---

## Task 1: Repo Scaffold & Docker Compose

**Files:**
- Create: `docker-compose.yml`
- Create: `CLAUDE.md`
- Create: `backend/FileScanner.Api/FileScanner.Api.csproj`
- Create: `frontend/package.json`
- Create: `ollama/entrypoint.sh`
- Create: `.gitignore`

- [ ] **Step 1: Initialize git and folder structure**

```bash
cd d:/Projeler/Demos/AIDemos/ClaudeDemos/file-scanner
git init
mkdir -p backend/FileScanner.Api/Controllers
mkdir -p backend/FileScanner.Api/Services
mkdir -p backend/FileScanner.Api/Models
mkdir -p frontend/src/components
mkdir -p frontend/src/services
mkdir -p frontend/src/types
mkdir -p ollama
```

- [ ] **Step 2: Create .gitignore**

```
# .gitignore
bin/
obj/
node_modules/
dist/
.env
*.user
.superpowers/
```

- [ ] **Step 3: Scaffold .NET project**

```bash
cd backend/FileScanner.Api
dotnet new webapi --no-openapi -n FileScanner.Api --output .
dotnet add package Tesseract --version 5.2.0
dotnet add package PdfPig --version 0.1.8
dotnet add package OllamaSharp --version 4.0.4
```

- [ ] **Step 4: Scaffold React/Vite project**

```bash
cd d:/Projeler/Demos/AIDemos/ClaudeDemos/file-scanner/frontend
npm create vite@latest . -- --template react-ts
npm install
npm install axios
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

- [ ] **Step 5: Configure Tailwind**

Edit `frontend/tailwind.config.js`:
```js
/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: { extend: {} },
  plugins: [],
}
```

Edit `frontend/src/index.css` (replace contents):
```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```

- [ ] **Step 6: Create Ollama entrypoint script**

```bash
# ollama/entrypoint.sh
#!/bin/sh
ollama serve &
sleep 5
ollama pull llava
wait
```

- [ ] **Step 7: Create docker-compose.yml**

```yaml
# docker-compose.yml
services:
  ollama:
    image: ollama/ollama:latest
    container_name: file-scanner-ollama
    ports:
      - "11434:11434"
    volumes:
      - ollama_data:/root/.ollama
      - ./ollama/entrypoint.sh:/entrypoint.sh
    entrypoint: ["/bin/sh", "/entrypoint.sh"]

  backend:
    build: ./backend/FileScanner.Api
    container_name: file-scanner-backend
    ports:
      - "5000:8080"
    environment:
      - OLLAMA_BASE_URL=http://ollama:11434
      - ASPNETCORE_ENVIRONMENT=Development
    depends_on:
      - ollama

  frontend:
    build: ./frontend
    container_name: file-scanner-frontend
    ports:
      - "3000:80"
    depends_on:
      - backend

volumes:
  ollama_data:
```

- [ ] **Step 8: Create CLAUDE.md**

```markdown
# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Start all services
docker-compose up --build

### Backend only (local dev)
cd backend/FileScanner.Api
dotnet run

### Frontend only (local dev)
cd frontend
npm run dev

### Run backend tests
cd backend/FileScanner.Api
dotnet test

### Run frontend type check
cd frontend
npm run build

## Architecture

Monorepo with three Docker services: frontend (React/Vite → Nginx, :3000), backend (.NET Core 8 API, :5000), ollama (LLaVA model, :11434).

### Backend flow
POST /api/scan → ScanController → ExtractionOrchestrator → [OcrService + LlmService in parallel] → best result by confidence score → ScanResult JSON

- OcrService: Tesseract.NET extracts raw text from image bytes
- LlmService: OllamaSharp sends image to LLaVA multimodal model, parses JSON response
- PdfService: PdfPig converts PDF pages to image byte arrays
- ExtractionOrchestrator: runs both services concurrently with Task.WhenAll, picks higher confidence

### Frontend flow
UploadZone (drag/drop) → scannerApi.ts POST → App state → ResultTable (grouped key-value) + DownloadButton (JSON file)

### Key environment variables
- OLLAMA_BASE_URL: Ollama server URL (default: http://localhost:11434)
```

- [ ] **Step 9: Commit scaffold**

```bash
cd d:/Projeler/Demos/AIDemos/ClaudeDemos/file-scanner
git add .
git commit -m "chore: initial monorepo scaffold with Docker Compose"
```

---

## Task 2: Backend Models

**Files:**
- Create: `backend/FileScanner.Api/Models/ScanResult.cs`
- Create: `backend/FileScanner.Api/Models/ExtractionCandidate.cs`

- [ ] **Step 1: Create ScanResult.cs**

```csharp
// backend/FileScanner.Api/Models/ScanResult.cs
namespace FileScanner.Api.Models;

public class ScanResult
{
    public DocumentInfo Document { get; set; } = new();
    public VendorInfo Vendor { get; set; } = new();
    public FinancialsInfo Financials { get; set; } = new();
    public List<LineItem> Items { get; set; } = [];
    public MetaInfo Meta { get; set; } = new();
}

public class DocumentInfo
{
    public string Type { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string InvoiceNo { get; set; } = string.Empty;
}

public class VendorInfo
{
    public string Name { get; set; } = string.Empty;
    public string TaxNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class FinancialsInfo
{
    public decimal Subtotal { get; set; }
    public decimal Vat { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "TRY";
    public string PaymentMethod { get; set; } = string.Empty;
}

public class LineItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class MetaInfo
{
    public double Confidence { get; set; }
    public string Source { get; set; } = string.Empty; // "ocr" | "llm" | "combined"
    public long ProcessingTimeMs { get; set; }
}
```

- [ ] **Step 2: Create ExtractionCandidate.cs**

```csharp
// backend/FileScanner.Api/Models/ExtractionCandidate.cs
namespace FileScanner.Api.Models;

public class ExtractionCandidate
{
    public ScanResult Result { get; set; } = new();
    public double Confidence { get; set; }
    public string Source { get; set; } = string.Empty;
}
```

- [ ] **Step 3: Commit**

```bash
git add backend/FileScanner.Api/Models/
git commit -m "feat: add ScanResult and ExtractionCandidate models"
```

---

## Task 3: PdfService

**Files:**
- Create: `backend/FileScanner.Api/Services/PdfService.cs`

- [ ] **Step 1: Create PdfService.cs**

```csharp
// backend/FileScanner.Api/Services/PdfService.cs
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FileScanner.Api.Services;

public class PdfService
{
    // Converts each PDF page to a PNG byte array using PdfPig + SkiaSharp rendering.
    // PdfPig does not render to bitmap natively; we extract page images embedded in the PDF.
    // For pages without embedded images, we return an empty array (will yield low confidence).
    public List<byte[]> ExtractPageImages(byte[] pdfBytes)
    {
        var results = new List<byte[]>();
        using var document = PdfDocument.Open(pdfBytes);
        foreach (var page in document.GetPages())
        {
            foreach (var image in page.GetImages())
            {
                try
                {
                    var bytes = image.RawBytes.ToArray();
                    if (bytes.Length > 0)
                        results.Add(bytes);
                }
                catch
                {
                    // skip unreadable embedded images
                }
            }
        }
        return results;
    }
}
```

- [ ] **Step 2: Register PdfService in Program.cs**

In `backend/FileScanner.Api/Program.cs`, add before `var app = builder.Build();`:
```csharp
builder.Services.AddSingleton<FileScanner.Api.Services.PdfService>();
```

- [ ] **Step 3: Commit**

```bash
git add backend/FileScanner.Api/Services/PdfService.cs backend/FileScanner.Api/Program.cs
git commit -m "feat: add PdfService for PDF page image extraction"
```

---

## Task 4: OcrService

**Files:**
- Create: `backend/FileScanner.Api/Services/OcrService.cs`

- [ ] **Step 1: Create OcrService.cs**

```csharp
// backend/FileScanner.Api/Services/OcrService.cs
using Tesseract;

namespace FileScanner.Api.Services;

public class OcrService
{
    private readonly string _tessDataPath;

    public OcrService(IWebHostEnvironment env)
    {
        // tessdata folder must exist at project root or be set via env var
        _tessDataPath = Environment.GetEnvironmentVariable("TESSDATA_PREFIX")
            ?? Path.Combine(env.ContentRootPath, "tessdata");
    }

    // Returns extracted plain text from image bytes. Empty string on failure.
    public string ExtractText(byte[] imageBytes)
    {
        try
        {
            using var engine = new TesseractEngine(_tessDataPath, "tur+eng", EngineMode.Default);
            using var ms = new MemoryStream(imageBytes);
            using var pix = Pix.LoadFromMemory(ms.ToArray());
            using var page = engine.Process(pix);
            return page.GetText() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
```

- [ ] **Step 2: Register OcrService in Program.cs**

```csharp
builder.Services.AddSingleton<FileScanner.Api.Services.OcrService>();
```

- [ ] **Step 3: Download Tesseract language data**

```bash
# Create tessdata folder and download Turkish + English trained data
mkdir -p backend/FileScanner.Api/tessdata
# Download from https://github.com/tesseract-ocr/tessdata
# tur.traineddata and eng.traineddata must be placed in tessdata/
```

Add to `backend/FileScanner.Api/FileScanner.Api.csproj`:
```xml
<ItemGroup>
  <None Update="tessdata/**">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

- [ ] **Step 4: Commit**

```bash
git add backend/FileScanner.Api/Services/OcrService.cs backend/FileScanner.Api/Program.cs backend/FileScanner.Api/FileScanner.Api.csproj
git commit -m "feat: add OcrService with Tesseract.NET (tur+eng)"
```

---

## Task 5: LlmService

**Files:**
- Create: `backend/FileScanner.Api/Services/LlmService.cs`

- [ ] **Step 1: Create LlmService.cs**

```csharp
// backend/FileScanner.Api/Services/LlmService.cs
using OllamaSharp;
using OllamaSharp.Models.Chat;
using System.Text.Json;
using FileScanner.Api.Models;

namespace FileScanner.Api.Services;

public class LlmService
{
    private readonly OllamaApiClient _client;
    private const string Model = "llava";

    public LlmService(IConfiguration config)
    {
        var baseUrl = config["OLLAMA_BASE_URL"] ?? "http://localhost:11434";
        _client = new OllamaApiClient(baseUrl);
    }

    // Sends image bytes directly to LLaVA multimodal model.
    // Returns (ScanResult, confidence) tuple. Confidence 0.0 on failure.
    public async Task<(ScanResult Result, double Confidence)> ExtractFromImageAsync(byte[] imageBytes)
    {
        try
        {
            var base64 = Convert.ToBase64String(imageBytes);
            var prompt = BuildPrompt();

            var chat = new Chat(_client);
            var messages = new List<Message>
            {
                new Message
                {
                    Role = ChatRole.User,
                    Content = prompt,
                    Images = [base64]
                }
            };

            var responseText = string.Empty;
            await foreach (var chunk in chat.SendAsync(messages, Model))
                responseText += chunk;

            return ParseResponse(responseText, "llm");
        }
        catch
        {
            return (new ScanResult(), 0.0);
        }
    }

    // Sends OCR-extracted text to Ollama for JSON structuring.
    public async Task<(ScanResult Result, double Confidence)> ExtractFromTextAsync(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return (new ScanResult(), 0.0);

        try
        {
            var prompt = $"{BuildPrompt()}\n\nOCR Text:\n{ocrText}";
            var chat = new Chat(_client);
            var messages = new List<Message>
            {
                new Message { Role = ChatRole.User, Content = prompt }
            };

            var responseText = string.Empty;
            await foreach (var chunk in chat.SendAsync(messages, "llama3"))
                responseText += chunk;

            return ParseResponse(responseText, "ocr");
        }
        catch
        {
            return (new ScanResult(), 0.0);
        }
    }

    private static string BuildPrompt() => """
        You are a receipt and invoice data extraction assistant.
        Extract ALL fields from the document and return ONLY valid JSON matching this exact schema:
        {
          "document": { "type": "", "date": "", "invoiceNo": "" },
          "vendor": { "name": "", "taxNo": "", "address": "", "phone": "", "email": "" },
          "financials": { "subtotal": 0, "vat": 0, "total": 0, "currency": "TRY", "paymentMethod": "" },
          "items": [{ "name": "", "quantity": 0, "unitPrice": 0, "lineTotal": 0 }]
        }
        Return ONLY the JSON object. No explanation, no markdown.
        """;

    private static (ScanResult Result, double Confidence) ParseResponse(string responseText, string source)
    {
        try
        {
            // Extract JSON from response (strip any markdown code fences)
            var json = responseText.Trim();
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start < 0 || end < 0) return (new ScanResult(), 0.0);
            json = json[start..(end + 1)];

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<ScanResult>(json, options) ?? new ScanResult();
            var confidence = CalculateConfidence(result);
            return (result, confidence);
        }
        catch
        {
            return (new ScanResult(), 0.0);
        }
    }

    // Confidence = ratio of non-empty required fields
    private static double CalculateConfidence(ScanResult result)
    {
        var filled = 0;
        var total = 6;
        if (!string.IsNullOrEmpty(result.Document.Type)) filled++;
        if (!string.IsNullOrEmpty(result.Document.Date)) filled++;
        if (!string.IsNullOrEmpty(result.Vendor.Name)) filled++;
        if (result.Financials.Total > 0) filled++;
        if (!string.IsNullOrEmpty(result.Financials.Currency)) filled++;
        if (result.Items.Count > 0) filled++;
        return Math.Round((double)filled / total, 2);
    }
}
```

- [ ] **Step 2: Register LlmService in Program.cs**

```csharp
builder.Services.AddSingleton<FileScanner.Api.Services.LlmService>();
```

- [ ] **Step 3: Commit**

```bash
git add backend/FileScanner.Api/Services/LlmService.cs backend/FileScanner.Api/Program.cs
git commit -m "feat: add LlmService with OllamaSharp (LLaVA + llama3)"
```

---

## Task 6: ExtractionOrchestrator

**Files:**
- Create: `backend/FileScanner.Api/Services/ExtractionOrchestrator.cs`

- [ ] **Step 1: Create ExtractionOrchestrator.cs**

```csharp
// backend/FileScanner.Api/Services/ExtractionOrchestrator.cs
using System.Diagnostics;
using FileScanner.Api.Models;

namespace FileScanner.Api.Services;

public class ExtractionOrchestrator
{
    private readonly OcrService _ocr;
    private readonly LlmService _llm;

    public ExtractionOrchestrator(OcrService ocr, LlmService llm)
    {
        _ocr = ocr;
        _llm = llm;
    }

    // Runs OCR→LLM text path and LLaVA image path in parallel.
    // Returns the result with the higher confidence score.
    public async Task<ScanResult> ProcessImageAsync(byte[] imageBytes)
    {
        var sw = Stopwatch.StartNew();

        // Run both paths concurrently
        var ocrTextTask = Task.Run(() => _ocr.ExtractText(imageBytes));
        var llmImageTask = _llm.ExtractFromImageAsync(imageBytes);

        await Task.WhenAll(ocrTextTask, llmImageTask);

        var ocrText = ocrTextTask.Result;
        var llmTextTask = _llm.ExtractFromTextAsync(ocrText);
        await llmTextTask;

        sw.Stop();

        var ocrCandidate = new ExtractionCandidate
        {
            Result = llmTextTask.Result.Result,
            Confidence = llmTextTask.Result.Confidence,
            Source = "ocr"
        };

        var llmCandidate = new ExtractionCandidate
        {
            Result = llmImageTask.Result.Result,
            Confidence = llmImageTask.Result.Confidence,
            Source = "llm"
        };

        var best = llmCandidate.Confidence >= ocrCandidate.Confidence ? llmCandidate : ocrCandidate;

        best.Result.Meta = new MetaInfo
        {
            Confidence = best.Confidence,
            Source = best.Source,
            ProcessingTimeMs = sw.ElapsedMilliseconds
        };

        return best.Result;
    }

    // For multi-page PDFs: process each page and merge results (first page wins for document/vendor/financials, items merged).
    public async Task<ScanResult> ProcessMultiPageAsync(List<byte[]> pages)
    {
        if (pages.Count == 0) return new ScanResult();
        if (pages.Count == 1) return await ProcessImageAsync(pages[0]);

        var tasks = pages.Select(ProcessImageAsync).ToList();
        var results = await Task.WhenAll(tasks);

        var primary = results[0];
        foreach (var subsequent in results.Skip(1))
            primary.Items.AddRange(subsequent.Items);

        return primary;
    }
}
```

- [ ] **Step 2: Register ExtractionOrchestrator in Program.cs**

```csharp
builder.Services.AddSingleton<FileScanner.Api.Services.ExtractionOrchestrator>();
```

- [ ] **Step 3: Commit**

```bash
git add backend/FileScanner.Api/Services/ExtractionOrchestrator.cs backend/FileScanner.Api/Program.cs
git commit -m "feat: add ExtractionOrchestrator with parallel OCR+LLM processing"
```

---

## Task 7: ScanController & Program.cs

**Files:**
- Create: `backend/FileScanner.Api/Controllers/ScanController.cs`
- Modify: `backend/FileScanner.Api/Program.cs`

- [ ] **Step 1: Create ScanController.cs**

```csharp
// backend/FileScanner.Api/Controllers/ScanController.cs
using Microsoft.AspNetCore.Mvc;
using FileScanner.Api.Models;
using FileScanner.Api.Services;

namespace FileScanner.Api.Controllers;

[ApiController]
[Route("api")]
public class ScanController : ControllerBase
{
    private readonly ExtractionOrchestrator _orchestrator;
    private readonly PdfService _pdf;
    private static readonly HashSet<string> AllowedExtensions = [".png", ".jpg", ".jpeg", ".webp", ".pdf"];
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20MB

    public ScanController(ExtractionOrchestrator orchestrator, PdfService pdf)
    {
        _orchestrator = orchestrator;
        _pdf = pdf;
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok" });

    [HttpPost("scan")]
    public async Task<ActionResult<ScanResult>> Scan(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Dosya yüklenmedi." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { error = "Dosya boyutu 20MB sınırını aşıyor." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { error = $"Desteklenmeyen format: {ext}. PNG, JPG, JPEG, WEBP veya PDF yükleyin." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var fileBytes = ms.ToArray();

        ScanResult result;
        if (ext == ".pdf")
        {
            var pages = _pdf.ExtractPageImages(fileBytes);
            if (pages.Count == 0)
                return StatusCode(500, new { error = "PDF'den görüntü çıkarılamadı." });
            result = await _orchestrator.ProcessMultiPageAsync(pages);
        }
        else
        {
            result = await _orchestrator.ProcessImageAsync(fileBytes);
        }

        return Ok(result);
    }
}
```

- [ ] **Step 2: Configure Program.cs**

Replace `backend/FileScanner.Api/Program.cs` with:
```csharp
using FileScanner.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddSingleton<PdfService>();
builder.Services.AddSingleton<OcrService>();
builder.Services.AddSingleton<LlmService>();
builder.Services.AddSingleton<ExtractionOrchestrator>();

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.Run();
```

- [ ] **Step 3: Test backend builds**

```bash
cd backend/FileScanner.Api
dotnet build
```
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/FileScanner.Api/Controllers/ backend/FileScanner.Api/Program.cs
git commit -m "feat: add ScanController with POST /api/scan and GET /api/health"
```

---

## Task 8: Backend Dockerfile

**Files:**
- Create: `backend/FileScanner.Api/Dockerfile`

- [ ] **Step 1: Create Dockerfile**

```dockerfile
# backend/FileScanner.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install Tesseract system library
RUN apt-get update && apt-get install -y libtesseract-dev tesseract-ocr tesseract-ocr-tur && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FileScanner.Api.dll"]
```

- [ ] **Step 2: Remove tessdata COPY step from csproj (handled by apt)**

Since Tesseract is installed via apt-get in Docker, tessdata is in `/usr/share/tesseract-ocr/`. Update `OcrService.cs` fallback path:

In `OcrService.cs`, update `_tessDataPath` fallback:
```csharp
_tessDataPath = Environment.GetEnvironmentVariable("TESSDATA_PREFIX")
    ?? "/usr/share/tesseract-ocr/5/tessdata";
```

- [ ] **Step 3: Commit**

```bash
git add backend/FileScanner.Api/Dockerfile backend/FileScanner.Api/Services/OcrService.cs
git commit -m "feat: add backend Dockerfile with Tesseract system install"
```

---

## Task 9: Frontend Types & API Service

**Files:**
- Create: `frontend/src/types/ScanResult.ts`
- Create: `frontend/src/services/scannerApi.ts`

- [ ] **Step 1: Create ScanResult.ts**

```typescript
// frontend/src/types/ScanResult.ts
export interface DocumentInfo {
  type: string;
  date: string;
  invoiceNo: string;
}

export interface VendorInfo {
  name: string;
  taxNo: string;
  address: string;
  phone: string;
  email: string;
}

export interface FinancialsInfo {
  subtotal: number;
  vat: number;
  total: number;
  currency: string;
  paymentMethod: string;
}

export interface LineItem {
  name: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface MetaInfo {
  confidence: number;
  source: string;
  processingTimeMs: number;
}

export interface ScanResult {
  document: DocumentInfo;
  vendor: VendorInfo;
  financials: FinancialsInfo;
  items: LineItem[];
  meta: MetaInfo;
}
```

- [ ] **Step 2: Create scannerApi.ts**

```typescript
// frontend/src/services/scannerApi.ts
import axios from 'axios';
import type { ScanResult } from '../types/ScanResult';

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

export async function scanFile(file: File): Promise<ScanResult> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await axios.post<ScanResult>(`${API_BASE}/api/scan`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });

  return response.data;
}
```

- [ ] **Step 3: Add VITE_API_URL to frontend env**

Create `frontend/.env`:
```
VITE_API_URL=http://localhost:5000
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/types/ frontend/src/services/ frontend/.env
git commit -m "feat: add frontend TypeScript types and scanner API service"
```

---

## Task 10: UploadZone Component

**Files:**
- Create: `frontend/src/components/UploadZone.tsx`

- [ ] **Step 1: Create UploadZone.tsx**

```tsx
// frontend/src/components/UploadZone.tsx
import { useRef, useState } from 'react';

interface Props {
  onFileSelected: (file: File) => void;
  previewUrl: string | null;
  onRemove: () => void;
  isLoading: boolean;
}

export function UploadZone({ onFileSelected, previewUrl, onRemove, isLoading }: Props) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);

  const handleFile = (file: File) => {
    const allowed = ['image/png', 'image/jpeg', 'image/webp', 'application/pdf'];
    if (allowed.includes(file.type)) onFileSelected(file);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files[0];
    if (file) handleFile(file);
  };

  return (
    <div className="flex flex-col items-center justify-center h-full">
      <div
        onClick={() => !previewUrl && inputRef.current?.click()}
        onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={handleDrop}
        className={`w-full border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-colors
          ${isDragging ? 'border-emerald-400 bg-emerald-50' : 'border-gray-300 hover:border-gray-400'}
          ${previewUrl ? 'cursor-default' : ''}`}
      >
        {!previewUrl ? (
          <>
            <div className="text-4xl text-gray-400 mb-3">☁</div>
            <p className="font-medium text-gray-700">Fiş veya Faturanızı Yükleyin</p>
            <p className="text-sm text-emerald-500 mt-1">PNG, JPG, PDF formatında. Tıklayın veya sürükleyip bırakın.</p>
          </>
        ) : (
          <img src={previewUrl} alt="Yüklenen görüntü" className="max-h-64 mx-auto rounded-lg object-contain" />
        )}
      </div>

      {previewUrl && (
        <button
          onClick={onRemove}
          className="mt-3 px-4 py-1.5 bg-red-500 text-white text-sm rounded-lg hover:bg-red-600 transition-colors"
        >
          Kaldır
        </button>
      )}

      {isLoading && (
        <p className="mt-4 text-sm text-gray-500 animate-pulse">İşleniyor...</p>
      )}

      <input
        ref={inputRef}
        type="file"
        accept=".png,.jpg,.jpeg,.webp,.pdf"
        className="hidden"
        onChange={(e) => e.target.files?.[0] && handleFile(e.target.files[0])}
      />
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/components/UploadZone.tsx
git commit -m "feat: add UploadZone component with drag-and-drop"
```

---

## Task 11: ResultTable & DownloadButton Components

**Files:**
- Create: `frontend/src/components/ResultTable.tsx`
- Create: `frontend/src/components/DownloadButton.tsx`

- [ ] **Step 1: Create ResultTable.tsx**

```tsx
// frontend/src/components/ResultTable.tsx
import type { ScanResult } from '../types/ScanResult';

interface Props {
  result: ScanResult;
}

interface Row {
  label: string;
  value: string | number;
  highlight?: boolean;
}

function buildRows(result: ScanResult): Row[] {
  const rows: Row[] = [
    { label: 'Belge Tipi', value: result.document.type },
    { label: 'Tarih', value: result.document.date },
    { label: 'Fatura No', value: result.document.invoiceNo, highlight: true },
    { label: 'İşletme Adı', value: result.vendor.name, highlight: true },
    { label: 'Vergi No', value: result.vendor.taxNo, highlight: true },
    { label: 'Adres', value: result.vendor.address },
    { label: 'Telefon', value: result.vendor.phone },
    { label: 'Ara Toplam', value: result.financials.subtotal ? `${result.financials.currency} ${result.financials.subtotal.toFixed(2)}` : '' },
    { label: `KDV`, value: result.financials.vat ? `${result.financials.currency} ${result.financials.vat.toFixed(2)}` : '' },
    { label: 'Toplam Tutar', value: result.financials.total ? `${result.financials.currency} ${result.financials.total.toFixed(2)}` : '', highlight: true },
    { label: 'Ödeme Yöntemi', value: result.financials.paymentMethod, highlight: true },
  ];
  return rows.filter(r => r.value !== '' && r.value !== 0);
}

export function ResultTable({ result }: Props) {
  const rows = buildRows(result);

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="bg-gray-100">
            <th className="text-left px-4 py-2 font-semibold text-gray-600 uppercase text-xs tracking-wider">ÖZELLİK</th>
            <th className="text-left px-4 py-2 font-semibold text-gray-600 uppercase text-xs tracking-wider">DEĞER</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.label} className="border-b border-gray-100 hover:bg-gray-50">
              <td className="px-4 py-2.5 text-gray-600">{row.label}</td>
              <td className={`px-4 py-2.5 font-medium ${row.highlight ? 'text-emerald-600' : 'text-gray-800'}`}>
                {String(row.value)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {result.items.length > 0 && (
        <div className="mt-4">
          <h4 className="text-xs font-semibold uppercase tracking-wider text-gray-500 px-4 mb-2">Kalemler</h4>
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-gray-100">
                <th className="text-left px-4 py-2 text-xs font-semibold text-gray-600">ÜRÜN</th>
                <th className="text-right px-4 py-2 text-xs font-semibold text-gray-600">ADET</th>
                <th className="text-right px-4 py-2 text-xs font-semibold text-gray-600">BİRİM FİYAT</th>
                <th className="text-right px-4 py-2 text-xs font-semibold text-gray-600">TOPLAM</th>
              </tr>
            </thead>
            <tbody>
              {result.items.map((item, i) => (
                <tr key={i} className="border-b border-gray-100">
                  <td className="px-4 py-2">{item.name}</td>
                  <td className="px-4 py-2 text-right">{item.quantity}</td>
                  <td className="px-4 py-2 text-right">{item.unitPrice.toFixed(2)}</td>
                  <td className="px-4 py-2 text-right font-medium text-emerald-600">{item.lineTotal.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="px-4 py-2 text-xs text-gray-400 mt-2">
        Kaynak: {result.meta.source} · Güven: %{Math.round(result.meta.confidence * 100)} · {result.meta.processingTimeMs}ms
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Create DownloadButton.tsx**

```tsx
// frontend/src/components/DownloadButton.tsx
import type { ScanResult } from '../types/ScanResult';

interface Props {
  result: ScanResult;
}

export function DownloadButton({ result }: Props) {
  const handleDownload = () => {
    const json = JSON.stringify(result, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `scan-${result.document.date || Date.now()}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <button
      onClick={handleDownload}
      className="flex items-center gap-2 px-4 py-2 bg-emerald-500 text-white text-sm font-medium rounded-lg hover:bg-emerald-600 transition-colors"
    >
      ⬇ JSON İndir
    </button>
  );
}
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/ResultTable.tsx frontend/src/components/DownloadButton.tsx
git commit -m "feat: add ResultTable and DownloadButton components"
```

---

## Task 12: App.tsx — Main Layout

**Files:**
- Modify: `frontend/src/App.tsx`

- [ ] **Step 1: Replace App.tsx**

```tsx
// frontend/src/App.tsx
import { useState } from 'react';
import { UploadZone } from './components/UploadZone';
import { ResultTable } from './components/ResultTable';
import { DownloadButton } from './components/DownloadButton';
import { scanFile } from './services/scannerApi';
import type { ScanResult } from './types/ScanResult';

export default function App() {
  const [file, setFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [result, setResult] = useState<ScanResult | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleFileSelected = async (selectedFile: File) => {
    setFile(selectedFile);
    setResult(null);
    setError(null);

    if (selectedFile.type !== 'application/pdf') {
      setPreviewUrl(URL.createObjectURL(selectedFile));
    } else {
      setPreviewUrl(null);
    }

    setIsLoading(true);
    try {
      const scanResult = await scanFile(selectedFile);
      setResult(scanResult);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Beklenmeyen bir hata oluştu.';
      setError(message);
    } finally {
      setIsLoading(false);
    }
  };

  const handleRemove = () => {
    setFile(null);
    setPreviewUrl(null);
    setResult(null);
    setError(null);
  };

  return (
    <div className="min-h-screen bg-white">
      {/* Header */}
      <header className="border-b border-gray-200 px-6 py-4">
        <h1 className="text-xl font-bold text-gray-900">Receipt & Invoice Scanner</h1>
        <p className="text-sm text-gray-500">Fiş ve faturalarınızı tarayıp verilerini çıkarın</p>
      </header>

      {/* Main content */}
      <main className="p-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 max-w-6xl mx-auto">
          {/* Left: Upload */}
          <div className="border border-dashed border-gray-200 rounded-xl p-6 min-h-80">
            <UploadZone
              onFileSelected={handleFileSelected}
              previewUrl={previewUrl}
              onRemove={handleRemove}
              isLoading={isLoading}
            />
          </div>

          {/* Right: Results */}
          <div className="border border-gray-200 rounded-xl overflow-hidden">
            <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200">
              <h2 className="font-semibold text-gray-700">Çıkarılan Bilgiler</h2>
              {result && <DownloadButton result={result} />}
            </div>

            {error && (
              <div className="p-4 text-sm text-red-600 bg-red-50">
                {error}
              </div>
            )}

            {result && <ResultTable result={result} />}

            {!result && !error && (
              <div className="p-8 text-center text-gray-400 text-sm">
                Sonuçlar burada görünecek
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
```

- [ ] **Step 2: Verify frontend builds**

```bash
cd frontend
npm run build
```
Expected: `dist/` folder created, no TypeScript errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/App.tsx
git commit -m "feat: add main App layout matching mockup design"
```

---

## Task 13: Frontend Dockerfile & Nginx Config

**Files:**
- Create: `frontend/Dockerfile`
- Create: `frontend/nginx.conf`

- [ ] **Step 1: Create nginx.conf**

```nginx
# frontend/nginx.conf
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://backend:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

- [ ] **Step 2: Create Dockerfile**

```dockerfile
# frontend/Dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

- [ ] **Step 3: Update vite.config.ts for production build**

```typescript
// frontend/vite.config.ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5000'
    }
  }
})
```

- [ ] **Step 4: Commit**

```bash
git add frontend/Dockerfile frontend/nginx.conf frontend/vite.config.ts
git commit -m "feat: add frontend Dockerfile and Nginx config"
```

---

## Task 14: End-to-End Test with Docker Compose

**Files:** none new — integration test

- [ ] **Step 1: Build and start all services**

```bash
cd d:/Projeler/Demos/AIDemos/ClaudeDemos/file-scanner
docker-compose up --build
```
Expected: All 3 containers start. Ollama pulls llava model (first run takes a few minutes).

- [ ] **Step 2: Test health endpoint**

```bash
curl http://localhost:5000/api/health
```
Expected: `{"status":"ok"}`

- [ ] **Step 3: Test frontend loads**

Open http://localhost:3000 in browser.
Expected: Upload zone + results panel visible.

- [ ] **Step 4: Test scan with a sample receipt image**

```bash
curl -X POST http://localhost:5000/api/scan \
  -F "file=@/path/to/sample-receipt.jpg" \
  -H "Accept: application/json"
```
Expected: JSON response with document, vendor, financials, items, meta fields populated.

- [ ] **Step 5: Test PDF upload**

```bash
curl -X POST http://localhost:5000/api/scan \
  -F "file=@/path/to/sample-invoice.pdf"
```
Expected: JSON response. If PDF has no embedded images, meta.confidence will be low.

- [ ] **Step 6: Test error cases**

```bash
# Wrong format
curl -X POST http://localhost:5000/api/scan -F "file=@README.md"
```
Expected: `400` with Turkish error message.

- [ ] **Step 7: Final commit**

```bash
git add .
git commit -m "chore: verify end-to-end Docker Compose integration"
```

---

## Self-Review Notes

**Spec coverage check:**
- ✅ React + Vite + TypeScript + Tailwind — Task 1, 9-13
- ✅ .NET Core 8 API — Task 1, 7
- ✅ Tesseract OCR — Task 4
- ✅ Ollama LLaVA multimodal — Task 5
- ✅ Parallel OCR + LLM — Task 6
- ✅ PDF multi-page — Task 3, 6
- ✅ Grouped JSON schema (document/vendor/financials/items/meta) — Task 2, 5
- ✅ Docker Compose 3 services — Task 1, 8, 13, 14
- ✅ Drag-and-drop upload, thumbnail, Kaldır button — Task 10
- ✅ ResultTable key-value display — Task 11
- ✅ JSON İndir button — Task 11
- ✅ Error handling (400/500, fallback) — Task 7, 5, 6
- ✅ Turkish UI labels — Task 10, 11, 12
- ✅ CLAUDE.md — Task 1
