# Receipt & Invoice Scanner

Fiş ve faturalardan yapılandırılmış veri çıkaran yerel (local-only) web uygulaması. Tamamen ücretsiz ve internet bağlantısı gerektirmez — OCR ve yapay zeka işlemleri kendi bilgisayarınızda çalışır.

## Özellikler 3333

- PNG, JPG, JPEG, WEBP ve çok sayfalı PDF desteği (maks. 20 MB)
- Tesseract OCR ve Ollama LLaVA paralel işleme, en yüksek güven skoru seçilir
- Çıkarılan veriler: belge tipi, tarih, fatura no, işletme bilgileri, KDV/toplam, kalem listesi
- Sonuçları JSON dosyası olarak indirme
- Türkçe arayüz

## Gereksinimler

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows/Mac/Linux)
- İlk çalıştırmada Ollama LLaVA modeli indirilir (~5 GB), disk alanı gerekir

## Kurulum ve Çalıştırma

```bash
git clone <repo-url>
cd file-scanner
docker-compose up --build
```

Servisler hazır olduğunda:

| Servis   | Adres                    |
|----------|--------------------------|
| Uygulama | http://localhost:3000    |
| API      | http://localhost:5000    |
| Ollama   | http://localhost:11434   |

> İlk çalıştırmada Ollama, LLaVA modelini indirir. İnternet hızına bağlı olarak 10-20 dakika sürebilir.

## Yerel Geliştirme (Docker olmadan)

### Backend

```bash
cd backend/FileScanner.Api
dotnet run
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Ön koşullar: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), [Node.js 20+](https://nodejs.org), Tesseract (`tur.traineddata` + `eng.traineddata` dosyaları `backend/FileScanner.Api/tessdata/` klasörüne yerleştirilmeli).

## Mimari

```
file-scanner/
├── frontend/          # React 18 + Vite + TypeScript + Tailwind CSS
├── backend/
│   └── FileScanner.Api/   # .NET Core 8 Web API
│       ├── Controllers/   # ScanController (POST /api/scan, GET /api/health)
│       ├── Services/      # OcrService, LlmService, PdfService, ExtractionOrchestrator
│       └── Models/        # ScanResult, ExtractionCandidate, ExtractionSource
├── ollama/            # Ollama model entrypoint
└── docker-compose.yml
```

**İşlem akışı:**

```
Dosya yükleme → POST /api/scan
  └─ PDF ise → PdfService (sayfa görselleri)
  └─ Paralel: OcrService (Tesseract) + LlmService (LLaVA)
  └─ ExtractionOrchestrator → güven skoru yüksek olan seçilir
  └─ JSON yanıt: document / vendor / financials / items / meta
```

## API

### `POST /api/scan`

Dosya yükler, yapılandırılmış JSON döndürür.

```bash
curl -X POST http://localhost:5000/api/scan \
  -F "file=@fatura.jpg"
```

**Yanıt şeması:**

```json
{
  "document":   { "type": "", "date": "", "invoiceNo": "" },
  "vendor":     { "name": "", "taxNo": "", "address": "", "phone": "", "email": "" },
  "financials": { "subtotal": 0, "vat": 0, "total": 0, "currency": "TRY", "paymentMethod": "" },
  "items":      [{ "name": "", "quantity": 0, "unitPrice": 0, "lineTotal": 0 }],
  "meta":       { "confidence": 0.95, "source": "llm", "processingTimeMs": 1240 }
}
```

### `GET /api/health`

```bash
curl http://localhost:5000/api/health
# → {"status":"ok"}
```

## Ortam Değişkenleri

| Değişken         | Varsayılan                  | Açıklama                  |
|------------------|-----------------------------|---------------------------|
| `OLLAMA_BASE_URL`| `http://localhost:11434`    | Ollama sunucu adresi      |
| `VITE_API_URL`   | `http://localhost:5000`     | Frontend API adresi       |
| `TESSDATA_PREFIX`| `/usr/share/tesseract-ocr/5/tessdata` | Tesseract veri yolu |

## Kapsam Dışı

- Kullanıcı kimlik doğrulama
- Tarama geçmişi veritabanı
- Toplu dosya yükleme
- Ücretli bulut OCR/LLM servisleri

## Lisans

MIT
