import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test/test-utils';
import { createMockApiClient, mockLatestRatesResponse } from '../test/mocks';
import LiveRatesSection from './LiveRatesSection';

// Mock the API client
vi.mock('../lib/api-client', () => ({
  apiClient: createMockApiClient(),
}));

describe('LiveRatesSection', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render live rates section', () => {
    renderWithProviders(<LiveRatesSection />);

    expect(screen.getByText('Live Exchange Rates')).toBeInTheDocument();
    expect(screen.getByText(/real-time rates from the market/i)).toBeInTheDocument();
  });

  it('should fetch and display latest rates for selected base currency', async () => {
    const { apiClient } = await import('../lib/api-client');
    
    renderWithProviders(<LiveRatesSection />);

    await waitFor(() => {
      expect(apiClient.getLatestRates).toHaveBeenCalledWith('USD');
    });

    await waitFor(() => {
      expect(screen.getByText('EUR')).toBeInTheDocument();
      expect(screen.getByText('GBP')).toBeInTheDocument();
      expect(screen.getByText('JPY')).toBeInTheDocument();
    });
  });

  it('should show loading state while fetching', async () => {
    const { apiClient } = await import('../lib/api-client');
    
    vi.mocked(apiClient.getLatestRates).mockImplementation(
      () => new Promise((resolve) => setTimeout(() => resolve(mockLatestRatesResponse), 100))
    );

    renderWithProviders(<LiveRatesSection />);

    expect(screen.getByText(/loading rates/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('EUR')).toBeInTheDocument();
    });
  });

  it('should handle API errors gracefully', async () => {
    const { apiClient } = await import('../lib/api-client');
    
    vi.mocked(apiClient.getLatestRates).mockRejectedValueOnce(new Error('API Error'));

    renderWithProviders(<LiveRatesSection />);

    await waitFor(() => {
      expect(screen.getByText(/failed to load exchange rates/i)).toBeInTheDocument();
    });
  });

  it('should refetch rates when refresh button clicked', async () => {
    const user = userEvent.setup();
    const { apiClient } = await import('../lib/api-client');
    
    renderWithProviders(<LiveRatesSection />);

    await waitFor(() => {
      expect(apiClient.getLatestRates).toHaveBeenCalledTimes(1);
    });

    const refreshButton = screen.getByTitle('Refresh rates');
    await user.click(refreshButton);

    await waitFor(() => {
      expect(apiClient.getLatestRates).toHaveBeenCalledTimes(2);
    });
  });

  it('should update rates when base currency is changed', async () => {
    const user = userEvent.setup();
    const { apiClient } = await import('../lib/api-client');
    
    renderWithProviders(<LiveRatesSection />);

    await waitFor(() => {
      expect(apiClient.getLatestRates).toHaveBeenCalledWith('USD');
    });

    const baseSelect = screen.getByRole('combobox');
    await user.selectOptions(baseSelect, 'EUR');

    await waitFor(() => {
      expect(apiClient.getLatestRates).toHaveBeenCalledWith('EUR');
    });
  });

  it('should display rate values correctly', async () => {
    renderWithProviders(<LiveRatesSection />);

    await waitFor(() => {
      expect(screen.getByText('0.8550')).toBeInTheDocument();
      expect(screen.getByText('0.7850')).toBeInTheDocument();
      expect(screen.getByText('110.5000')).toBeInTheDocument();
    });
  });
});
