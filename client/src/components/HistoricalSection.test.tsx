import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test/test-utils';
import { createMockApiClient, mockHistoricalRatesResponse } from '../test/mocks';
import HistoricalSection from './HistoricalSection';

// Mock the API client
vi.mock('../lib/api-client', () => ({
  apiClient: createMockApiClient(),
}));

// Mock date-fns to have consistent dates in tests
vi.mock('date-fns', async () => {
  const actual = await vi.importActual('date-fns');
  return {
    ...actual,
    format: (date: Date, formatStr: string) => {
      if (formatStr === 'yyyy-MM-dd') {
        return date.toISOString().split('T')[0];
      }
      if (formatStr === 'MMM dd, yyyy') {
        return date.toLocaleDateString('en-US', { month: 'short', day: '2-digit', year: 'numeric' });
      }
      return date.toString();
    },
    subDays: (date: Date, days: number) => {
      const result = new Date(date);
      result.setDate(result.getDate() - days);
      return result;
    },
  };
});

describe('HistoricalSection', () => {
  beforeEach(async () => {
    vi.clearAllMocks();
    // Reset the mock to return default response for each test
    const { apiClient } = await import('../lib/api-client');
    vi.mocked(apiClient.getHistoricalRates).mockResolvedValue(mockHistoricalRatesResponse);
  });

  it('should render historical rates section', () => {
    renderWithProviders(<HistoricalSection />);

    expect(screen.getByText('Historical Exchange Rates')).toBeInTheDocument();
    expect(screen.getByText(/browse past exchange rate data/i)).toBeInTheDocument();
  });

  it('should fetch and display historical rates', async () => {
    const { apiClient } = await import('../lib/api-client');
    
    renderWithProviders(<HistoricalSection />);

    await waitFor(() => {
      expect(apiClient.getHistoricalRates).toHaveBeenCalled();
    });

    await waitFor(() => {
      const table = screen.getByRole('table');
      expect(table).toBeInTheDocument();
    });
  });

  it('should show loading state while fetching', async () => {
    const { apiClient } = await import('../lib/api-client');
    
    vi.mocked(apiClient.getHistoricalRates).mockImplementation(
      () => new Promise((resolve) => setTimeout(() => resolve(mockHistoricalRatesResponse), 100))
    );

    renderWithProviders(<HistoricalSection />);

    expect(screen.getByText(/loading historical data/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByRole('table')).toBeInTheDocument();
    });
  });

  it('should handle API errors gracefully', async () => {
    const { apiClient } = await import('../lib/api-client');
    
    vi.mocked(apiClient.getHistoricalRates).mockRejectedValueOnce(new Error('API Error'));

    renderWithProviders(<HistoricalSection />);

    await waitFor(() => {
      expect(screen.getByText(/failed to load historical rates/i)).toBeInTheDocument();
    });
  });

  it('should paginate through results', async () => {
    const user = userEvent.setup();
    const { apiClient } = await import('../lib/api-client');
    
    // Mock response with multiple pages
    const multiPageResponse = {
      ...mockHistoricalRatesResponse,
      totalPages: 3,
      totalItems: 30,
    };
    vi.mocked(apiClient.getHistoricalRates).mockResolvedValue(multiPageResponse);

    renderWithProviders(<HistoricalSection />);

    await waitFor(() => {
      expect(screen.getByRole('table')).toBeInTheDocument();
    });

    // Check page 1 is active
    const page1Button = screen.getByRole('button', { name: '1' });
    expect(page1Button).toHaveClass('bg-teal-500');

    // Click page 2
    const page2Button = screen.getByRole('button', { name: '2' });
    await user.click(page2Button);

    await waitFor(() => {
      expect(apiClient.getHistoricalRates).toHaveBeenCalledWith(
        expect.any(String),
        expect.any(String),
        expect.any(String),
        2,
        10
      );
    });
  });

  it('should reset to page 1 when filters change', async () => {
    const user = userEvent.setup();
    const { apiClient } = await import('../lib/api-client');
    
    // Mock response with multiple pages
    const multiPageResponse = {
      ...mockHistoricalRatesResponse,
      totalPages: 3,
    };
    vi.mocked(apiClient.getHistoricalRates).mockResolvedValue(multiPageResponse);

    renderWithProviders(<HistoricalSection />);

    await waitFor(() => {
      expect(screen.getByRole('table')).toBeInTheDocument();
    });

    // Go to page 2
    const page2Button = screen.getByRole('button', { name: '2' });
    await user.click(page2Button);

    await waitFor(() => {
      const calls = vi.mocked(apiClient.getHistoricalRates).mock.calls;
      const lastCall = calls[calls.length - 1];
      expect(lastCall[3]).toBe(2); // page parameter
    });

    // Change base currency
    const baseSelect = screen.getByLabelText(/base currency/i);
    await user.selectOptions(baseSelect, 'EUR');

    // Should reset to page 1
    await waitFor(() => {
      const calls = vi.mocked(apiClient.getHistoricalRates).mock.calls;
      const lastCall = calls[calls.length - 1];
      expect(lastCall[0]).toBe('EUR'); // baseCurrency
      expect(lastCall[3]).toBe(1); // page parameter should be 1
    }, { timeout: 2000 });
  });

  it('should display pagination info correctly', async () => {
    renderWithProviders(<HistoricalSection />);

    await waitFor(() => {
      expect(screen.getByText(/showing 1 to 3 of 3 entries/i)).toBeInTheDocument();
    });
  });

  it('should disable previous button on first page', async () => {
    renderWithProviders(<HistoricalSection />);

    await waitFor(() => {
      const prevButton = screen.getAllByRole('button').find(btn => 
        btn.querySelector('svg')?.classList.contains('lucide-chevron-left')
      );
      expect(prevButton).toBeDisabled();
    });
  });

  it('should disable next button on last page', async () => {
    renderWithProviders(<HistoricalSection />);

    // Wait for data to load and pagination to render
    await waitFor(() => {
      expect(screen.getByRole('table')).toBeInTheDocument();
    });

    // Wait a bit more to ensure pagination buttons are rendered
    await waitFor(() => {
      expect(screen.getByText(/showing 1 to 3 of 3 entries/i)).toBeInTheDocument();
    });

    // Find the "Next page" button by aria-label and verify it's disabled
    // Since mockHistoricalRatesResponse has totalPages: 1 and we're on page 1, next should be disabled
    await waitFor(() => {
      const nextButton = screen.getByRole('button', { name: /next page/i });
      expect(nextButton).toBeDisabled();
    });
  });

  it('should update query when date range changes', async () => {
    const user = userEvent.setup();
    const { apiClient } = await import('../lib/api-client');
    
    renderWithProviders(<HistoricalSection />);

    await waitFor(() => {
      expect(apiClient.getHistoricalRates).toHaveBeenCalled();
    });

    vi.clearAllMocks();

    const startDateInput = screen.getByLabelText(/start date/i);
    await user.clear(startDateInput);
    await user.type(startDateInput, '2024-01-01');

    await waitFor(() => {
      expect(apiClient.getHistoricalRates).toHaveBeenCalled();
    }, { timeout: 2000 });
  });

  it('should display currency codes in table header', async () => {
    renderWithProviders(<HistoricalSection />);

    await waitFor(() => {
      const table = screen.getByRole('table');
      const headers = within(table).getAllByRole('columnheader');
      
      expect(headers[0]).toHaveTextContent('Date');
      expect(headers[1]).toHaveTextContent('EUR');
      expect(headers[2]).toHaveTextContent('GBP');
      expect(headers[3]).toHaveTextContent('JPY');
    });
  });

  it('should display rate values in table cells', async () => {
    renderWithProviders(<HistoricalSection />);

    await waitFor(() => {
      expect(screen.getByText('0.8550')).toBeInTheDocument();
      expect(screen.getByText('0.7850')).toBeInTheDocument();
      expect(screen.getByText('110.5000')).toBeInTheDocument();
    });
  });
});
