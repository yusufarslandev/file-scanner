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
