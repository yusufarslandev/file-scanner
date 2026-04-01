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
  ocrText?: string;
}
