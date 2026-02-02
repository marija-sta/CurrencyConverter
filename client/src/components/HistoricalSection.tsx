import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Calendar, ChevronLeft, ChevronRight } from 'lucide-react';
import { apiClient } from '../lib/api-client';
import { CURRENCIES } from '../types/api';
import type { HistoricalRatesResponse } from '../types/api';
import { format, subDays } from 'date-fns';

export default function HistoricalSection() {
  const [baseCurrency, setBaseCurrency] = useState('USD');
  const [startDate, setStartDate] = useState(format(subDays(new Date(), 30), 'yyyy-MM-dd'));
  const [endDate, setEndDate] = useState(format(new Date(), 'yyyy-MM-dd'));
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const { data, isLoading, isError } = useQuery<HistoricalRatesResponse>({
    queryKey: ['historicalRates', baseCurrency, startDate, endDate, page],
    queryFn: () => apiClient.getHistoricalRates(baseCurrency, startDate, endDate, page, pageSize),
  });

  const goToPage = (newPage: number) => {
    if (data && newPage >= 1 && newPage <= data.totalPages) {
      setPage(newPage);
    }
  };

  // Get unique currency codes from all rates
  const currencyCodes = data?.rates && data.rates.length > 0 && data.rates[0]?.rates
    ? Object.keys(data.rates[0].rates).slice(0, 6)
    : [];

  return (
    <div>
      <div className="mb-8">
        <h2 className="text-3xl font-bold text-white mb-2">Historical Exchange Rates</h2>
        <p className="text-white/70">Browse past exchange rate data</p>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg p-6 mb-6">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label htmlFor="base-currency" className="block text-sm text-gray-600 mb-2">Base Currency</label>
            <select
              id="base-currency"
              value={baseCurrency}
              onChange={(e) => {
                setBaseCurrency(e.target.value);
                setPage(1);
              }}
              className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 cursor-pointer"
            >
              {CURRENCIES.map((currency) => (
                <option key={currency.code} value={currency.code}>
                  {currency.code} {currency.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label htmlFor="start-date" className="block text-sm text-gray-600 mb-2">Start Date</label>
            <div className="relative">
              <Calendar className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                id="start-date"
                type="date"
                value={startDate}
                max={new Date().toISOString().split('T')[0]}
                onChange={(e) => {
                  setStartDate(e.target.value);
                  setPage(1);
                }}
                className="w-full pl-10 pr-4 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500"
              />
            </div>
          </div>

          <div>
            <label htmlFor="end-date" className="block text-sm text-gray-600 mb-2">End Date</label>
            <div className="relative">
              <Calendar className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                id="end-date"
                type="date"
                value={endDate}
                max={new Date().toISOString().split('T')[0]}
                onChange={(e) => {
                  setEndDate(e.target.value);
                  setPage(1);
                }}
                className="w-full pl-10 pr-4 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500"
              />
            </div>
          </div>
        </div>
      </div>

      {/* Results */}
      {isLoading && (
        <div className="text-center text-white py-12">Loading historical data...</div>
      )}

      {isError && (
        <div className="bg-red-500/20 border border-red-500/50 rounded-lg p-4 text-red-200">
          Failed to load historical rates
        </div>
      )}

      {data && data.rates.length > 0 && (
        <>
          <div className="bg-white rounded-lg overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Date
                    </th>
                    {currencyCodes.map((code) => (
                      <th
                        key={code}
                        className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                      >
                        {code}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {data.rates.map((ratePoint) => (
                    <tr key={ratePoint.date} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                        {format(new Date(ratePoint.date), 'MMM dd, yyyy')}
                      </td>
                      {currencyCodes.map((code) => (
                        <td key={code} className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 font-mono">
                          {ratePoint.rates[code]?.toFixed(4) || '-'}
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Pagination */}
          <div className="mt-6 flex items-center justify-between">
            <div className="text-white/70 text-sm">
              Showing {((page - 1) * pageSize) + 1} to {Math.min(page * pageSize, data.totalItems)} of {data.totalItems} entries
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => goToPage(page - 1)}
                disabled={page === 1}
                aria-label="Previous page"
                className="px-3 py-2 border border-white/20 rounded-lg hover:bg-white/10 disabled:opacity-50 disabled:cursor-not-allowed text-white transition-colors"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              {[...Array(Math.min(3, data.totalPages))].map((_, i) => {
                const pageNum = i + 1;
                return (
                  <button
                    key={pageNum}
                    onClick={() => goToPage(pageNum)}
                    className={`px-4 py-2 rounded-lg transition-colors ${
                      page === pageNum
                        ? 'bg-teal-500 text-white'
                        : 'border border-white/20 text-white hover:bg-white/10'
                    }`}
                  >
                    {pageNum}
                  </button>
                );
              })}
              {data.totalPages > 3 && page < data.totalPages && (
                <>
                  <span className="text-white/50">...</span>
                  <button
                    onClick={() => goToPage(data.totalPages)}
                    className={`px-4 py-2 rounded-lg transition-colors ${
                      page === data.totalPages
                        ? 'bg-teal-500 text-white'
                        : 'border border-white/20 text-white hover:bg-white/10'
                    }`}
                  >
                    {data.totalPages}
                  </button>
                </>
              )}
              <button
                onClick={() => goToPage(page + 1)}
                disabled={page === data.totalPages}
                aria-label="Next page"
                className="px-3 py-2 border border-white/20 rounded-lg hover:bg-white/10 disabled:opacity-50 disabled:cursor-not-allowed text-white transition-colors"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
