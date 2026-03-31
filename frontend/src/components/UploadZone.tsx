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
