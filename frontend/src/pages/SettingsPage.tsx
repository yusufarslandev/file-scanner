import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getMe, saveApiKey, logout } from '../services/api';

export function SettingsPage() {
  const navigate = useNavigate();
  const [apiKey, setApiKey] = useState('');
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [user, setUser] = useState<{ email: string; hasApiKey: boolean } | null>(null);

  useEffect(() => {
    getMe()
      .then(setUser)
      .catch(() => {
        logout();
        navigate('/login');
      });
  }, [navigate]);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setMessage(null);

    try {
      await saveApiKey(apiKey);
      setMessage('API key kaydedildi!');
      setApiKey('');
      setUser(prev => prev ? { ...prev, hasApiKey: true } : null);
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'Hata oluştu');
    } finally {
      setSaving(false);
    }
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  if (!user) return null;

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-2xl mx-auto">
        <div className="flex items-center justify-between mb-6">
          <h1 className="text-xl font-bold text-gray-900">Ayarlar</h1>
          <button
            onClick={handleLogout}
            className="text-sm text-red-600 hover:underline"
          >
            Çıkış yap
          </button>
        </div>

        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-4">
          <h2 className="font-semibold text-gray-800 mb-4">Hesap Bilgileri</h2>
          <div className="text-sm text-gray-600">
            <p><span className="font-medium">E-posta:</span> {user.email}</p>
            <p><span className="font-medium">API Key:</span> {user.hasApiKey ? '✅ Kayıtlı' : '❌ Eklenmemiş'}</p>
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
          <h2 className="font-semibold text-gray-800 mb-4">9Router API Key</h2>
          <p className="text-sm text-gray-500 mb-4">
            Fatura tarama için 9Router API key'inizi girin. 
            <a href="https://9router.mezabilisim.com" target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline ml-1">
              Buradan alabilirsiniz
            </a>
          </p>

          <form onSubmit={handleSave} className="space-y-4">
            {message && (
              <div className={`p-3 text-sm rounded-lg ${message.includes('kaydedildi') ? 'bg-green-50 text-green-600' : 'bg-red-50 text-red-600'}`}>
                {message}
              </div>
            )}

            <div>
              <input
                type="password"
                value={apiKey}
                onChange={(e) => setApiKey(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                placeholder="sk-..."
                required
              />
            </div>

            <button
              type="submit"
              disabled={saving || !apiKey}
              className="w-full py-2 px-4 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {saving ? 'Kaydediliyor...' : 'API Key Kaydet'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}