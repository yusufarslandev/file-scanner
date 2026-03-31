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
