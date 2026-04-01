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

      {result.ocrText && (
        <div className="mt-4 px-4">
          <details className="cursor-pointer">
            <summary className="text-xs font-semibold uppercase tracking-wider text-gray-500 pb-2">OCR Ham Metin</summary>
            <pre className="bg-gray-50 p-3 rounded text-xs max-h-48 overflow-y-auto whitespace-pre-wrap break-words font-mono text-gray-700">
              {result.ocrText}
            </pre>
          </details>
        </div>
      )}
    </div>
  );
}
