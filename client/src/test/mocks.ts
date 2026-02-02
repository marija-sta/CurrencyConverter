import { vi } from 'vitest';
import type { ConversionResponse, LatestRatesResponse, HistoricalRatesResponse } from '../types/api';

export const mockConversionResponse: ConversionResponse = {
  from: 'USD',
  to: 'EUR',
  amount: 100,
  convertedAmount: 85.50,
  rate: 0.8550,
  date: '2024-01-15',
};

export const mockLatestRatesResponse: LatestRatesResponse = {
  baseCurrency: 'USD',
  date: '2024-01-15',
  rates: {
    EUR: 0.8550,
    GBP: 0.7850,
    JPY: 110.50,
    CAD: 1.2500,
    AUD: 1.3500,
  },
};

export const mockHistoricalRatesResponse: HistoricalRatesResponse = {
  baseCurrency: 'USD',
  startDate: '2024-01-01',
  endDate: '2024-01-10',
  rates: [
    {
      date: '2024-01-01',
      rates: { EUR: 0.8550, GBP: 0.7850, JPY: 110.50 },
    },
    {
      date: '2024-01-02',
      rates: { EUR: 0.8560, GBP: 0.7860, JPY: 110.60 },
    },
    {
      date: '2024-01-03',
      rates: { EUR: 0.8570, GBP: 0.7870, JPY: 110.70 },
    },
  ],
  page: 1,
  pageSize: 10,
  totalItems: 3,
  totalPages: 1,
};

export function createMockApiClient() {
  return {
    convert: vi.fn().mockResolvedValue(mockConversionResponse),
    getLatestRates: vi.fn().mockResolvedValue(mockLatestRatesResponse),
    getHistoricalRates: vi.fn().mockResolvedValue(mockHistoricalRatesResponse),
    getDevToken: vi.fn().mockResolvedValue('mock-token'),
    setToken: vi.fn(),
    getToken: vi.fn().mockReturnValue('mock-token'),
    clearToken: vi.fn(),
  };
}
