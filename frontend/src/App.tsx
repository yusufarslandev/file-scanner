import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useState, useEffect } from 'react';
import { LoginPage } from './pages/LoginPage';
import { SettingsPage } from './pages/SettingsPage';
import { UploadZone } from './components/UploadZone';
import { ResultTable } from './components/ResultTable';
import { DownloadButton } from './components/DownloadButton';
import { scanFile, getMe, logout } from './services/api';
import type { ScanResult } from './types/ScanResult';

function AppContent() {
  const [file, setFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [result, setResult] = useState<ScanResult | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [user, setUser] = useState<{ email: string; hasApiKey: boolean } | null>(null);
  const [checkingAuth, setCheckingAuth] = useState(true);

  useEffect(() => {
    getMe()
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setCheckingAuth(false));
  }, []);

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

  const handleLogout = () => {
    logout();
    setUser(null);
  };

  if (checkingAuth) {
    return (
      <div className="min-h-screen bg-white flex items-center justify-center">
        <div className="text-gray-400">Yükleniyor...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-white">
      {/* Header */}
      <header className="border-b border-gray-200 px-6 py-4">
        <div className="flex items-center justify-between max-w-6xl mx-auto">
          <div>
            <h1 className="text-xl font-bold text-gray-900">Receipt & Invoice Scanner</h1>
            <p className="text-sm text-gray-500">Fiş ve faturalarınızı tarayıp verilerini çıkarın</p>
          </div>
          <div className="flex items-center gap-4">
            {user && (
              <>
                <span className="text-sm text-gray-500">{user.email}</span>
                <a href="/settings" className="text-sm text-blue-600 hover:underline">Ayarlar</a>
                <button onClick={handleLogout} className="text-sm text-red-600 hover:underline">Çıkış</button>
              </>
            )}
          </div>
        </div>
      </header>

      {/* Main content */}
      <main className="p-6">
        {!user?.hasApiKey ? (
          <div className="max-w-2xl mx-auto">
            <div className="bg-yellow-50 border border-yellow-200 rounded-xl p-6 text-center">
              <h2 className="text-lg font-semibold text-yellow-800 mb-2">API Key Gerekli</h2>
              <p className="text-yellow-700 mb-4">
                Fatura tarama özelliğini kullanabilmek için 9Router API key'inizi eklemelisiniz.
              </p>
              <a href="/settings" className="inline-block py-2 px-4 bg-yellow-500 text-white font-medium rounded-lg hover:bg-yellow-600 transition-colors">
                API Key Ekle
              </a>
            </div>
          </div>
        ) : (
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
        )}
      </main>
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/settings" element={<SettingsPage />} />
        <Route path="/" element={<AppContent />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}