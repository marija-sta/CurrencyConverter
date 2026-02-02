import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { RefreshCw, TrendingUp, TrendingDown } from 'lucide-react';
import { apiClient } from '../lib/api-client';
import { CURRENCIES } from '../types/api';
import type { LatestRatesResponse } from '../types/api';

export default function LiveRatesSection() {
  const [baseCurrency, setBaseCurrency] = useState('USD');

  const { data, isLoading, isError, refetch } = useQuery<LatestRatesResponse>({
    queryKey: ['latestRates', baseCurrency],
    queryFn: () => apiClient.getLatestRates(baseCurrency),
  });

  const getRateChangeIndicator = () => {
    // Simulated change for visual purposes
    const change = (Math.random() - 0.5) * 2;
    return {
      isPositive: change > 0,
      percentage: Math.abs(change).toFixed(2),
    };
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-8">
        <div>
          <h2 className="text-3xl font-bold text-white mb-2">Live Exchange Rates</h2>
          <p className="text-white/70">Real-time rates from the market</p>
        </div>
        <div className="flex items-center gap-4">
          <label className="text-white/80 text-sm">Base currency:</label>
          <select
            value={baseCurrency}
            onChange={(e) => setBaseCurrency(e.target.value)}
            className="px-4 py-2 border border-white/20 rounded-lg bg-white/10 backdrop-blur-sm text-white focus:outline-none focus:ring-2 focus:ring-teal-500 cursor-pointer"
          >
            {CURRENCIES.map((currency) => (
              <option key={currency.code} value={currency.code} className="bg-gray-800">
                {currency.code} {currency.name}
              </option>
            ))}
          </select>
          <button
            onClick={() => refetch()}
            className="p-2 hover:bg-white/10 rounded-lg transition-colors text-white"
            title="Refresh rates"
          >
            <RefreshCw className="w-5 h-5" />
          </button>
        </div>
      </div>

      {isLoading && (
        <div className="text-center text-white py-12">Loading rates...</div>
      )}

      {isError && (
        <div className="bg-red-500/20 border border-red-500/50 rounded-lg p-4 text-red-200">
          Failed to load exchange rates
        </div>
      )}

      {data && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
          {Object.entries(data.rates).map(([currency, rate]) => {
            const change = getRateChangeIndicator();
            return (
              <div
                key={currency}
                className="bg-white/10 backdrop-blur-sm rounded-lg p-4 border border-white/10 hover:bg-white/15 transition-colors"
              >
                <div className="flex items-center justify-between mb-2">
                  <span className="text-white font-semibold text-lg">{currency}</span>
                  <span
                    className={`text-xs flex items-center gap-1 ${
                      change.isPositive ? 'text-green-400' : 'text-red-400'
                    }`}
                  >
                    {change.isPositive ? <TrendingUp className="w-3 h-3" /> : <TrendingDown className="w-3 h-3" />}
                    {change.percentage}%
                  </span>
                </div>
                <div className="text-2xl font-bold text-white mb-1">
                  {rate.toFixed(4)}
                </div>
                <div className="text-white/60 text-xs">
                  1 {baseCurrency} = {rate.toFixed(4)} {currency}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
