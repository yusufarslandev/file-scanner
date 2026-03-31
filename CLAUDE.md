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

- OLLAMA_BASE_URL: Ollama server URL (default: `http://localhost:11434`)
