import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { ArrowLeftRight } from 'lucide-react';
import { apiClient } from '../lib/api-client';
import { CURRENCIES } from '../types/api';
import type { ConversionResponse } from '../types/api';

export default function ConverterSection() {
  const [amount, setAmount] = useState('1000');
  const [fromCurrency, setFromCurrency] = useState('USD');
  const [toCurrency, setToCurrency] = useState('EUR');
  const [result, setResult] = useState<ConversionResponse | null>(null);

  const convertMutation = useMutation({
    mutationFn: () => apiClient.convert(Number(amount), fromCurrency, toCurrency),
    onSuccess: (data) => {
      setResult(data);
    },
  });

  const handleConvert = () => {
    convertMutation.mutate();
  };

  const swapCurrencies = () => {
    setFromCurrency(toCurrency);
    setToCurrency(fromCurrency);
    setResult(null);
  };

  return (
    <div className="max-w-4xl mx-auto">
      <div className="bg-white rounded-2xl shadow-xl p-8">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
          {/* You send */}
          <div>
            <label className="block text-sm text-gray-600 mb-2">You send</label>
            <div className="flex gap-2">
              <div className="relative flex-1">
                <span className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400 text-lg">$</span>
                <input
                  type="number"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  className="w-full pl-8 pr-4 py-3 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 text-lg"
                  placeholder="1000"
                />
              </div>
              <select
                value={fromCurrency}
                onChange={(e) => setFromCurrency(e.target.value)}
                className="px-4 py-3 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 bg-white cursor-pointer"
              >
                {CURRENCIES.map((currency) => (
                  <option key={currency.code} value={currency.code}>
                    {currency.code} {currency.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* They receive */}
          <div>
            <label className="block text-sm text-gray-600 mb-2">They receive</label>
            <div className="flex gap-2">
              <div className="relative flex-1">
                <span className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400 text-lg">€</span>
                <input
                  type="text"
                  value={result ? result.convertedAmount.toFixed(2) : amount}
                  readOnly
                  className="w-full pl-8 pr-4 py-3 border border-gray-200 rounded-lg bg-gray-50 text-lg"
                />
              </div>
              <select
                value={toCurrency}
                onChange={(e) => setToCurrency(e.target.value)}
                className="px-4 py-3 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 bg-white cursor-pointer"
              >
                {CURRENCIES.map((currency) => (
                  <option key={currency.code} value={currency.code}>
                    {currency.code} {currency.name}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>

        {/* Swap button */}
        <div className="flex justify-center -my-3 relative z-10">
          <button
            onClick={swapCurrencies}
            className="bg-white border-2 border-gray-200 rounded-full p-2 hover:border-teal-500 hover:text-teal-500 transition-colors"
          >
            <ArrowLeftRight className="w-5 h-5" />
          </button>
        </div>

        {/* Exchange rate info */}
        {result && (
          <div className="border-t border-gray-100 pt-6 mt-6">
            <div className="flex justify-between items-center text-sm">
              <span className="text-gray-600">Exchange rate</span>
              <span className="text-gray-900 font-medium">
                Last updated
              </span>
            </div>
            <div className="flex justify-between items-center mt-1">
              <span className="text-gray-900 font-semibold">
                1 {fromCurrency} = {result.rate.toFixed(4)} {toCurrency}
              </span>
              <span className="text-gray-500 text-sm">
                {new Date(result.date).toLocaleTimeString()}
              </span>
            </div>
          </div>
        )}

        {/* Convert button */}
        <button
          onClick={handleConvert}
          disabled={convertMutation.isPending}
          className="w-full mt-6 bg-gradient-to-r from-teal-500 to-cyan-500 text-white py-4 rounded-lg font-semibold hover:from-teal-600 hover:to-cyan-600 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {convertMutation.isPending ? 'Converting...' : 'Convert Now'}
        </button>

        {convertMutation.isError && (
          <div className="mt-4 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
            {convertMutation.error instanceof Error
              ? convertMutation.error.message
              : 'Failed to convert currency'}
          </div>
        )}
      </div>
    </div>
  );
}
