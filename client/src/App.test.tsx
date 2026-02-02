import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from './test/test-utils';
import App from './App';

// Mock components to simplify App testing
vi.mock('./components/ConverterSection', () => ({
  default: () => <div data-testid="converter-section">Converter Section</div>,
}));

vi.mock('./components/LiveRatesSection', () => ({
  default: () => <div data-testid="live-rates-section">Live Rates Section</div>,
}));

vi.mock('./components/HistoricalSection', () => ({
  default: () => <div data-testid="historical-section">Historical Section</div>,
}));

// Mock API client
vi.mock('./lib/api-client', () => ({
  apiClient: {
    getDevToken: vi.fn().mockResolvedValue('mock-token'),
    setToken: vi.fn(),
    getToken: vi.fn().mockReturnValue('mock-token'),
    clearToken: vi.fn(),
  },
}));

describe('App Integration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should authenticate and display all sections', async () => {
    const { apiClient } = await import('./lib/api-client');
    
    renderWithProviders(<App />);

    await waitFor(() => {
      expect(apiClient.getDevToken).toHaveBeenCalledWith('demo-user', ['admin']);
    });

    await waitFor(() => {
      expect(screen.getByTestId('converter-section')).toBeInTheDocument();
      expect(screen.getByTestId('live-rates-section')).toBeInTheDocument();
      expect(screen.getByTestId('historical-section')).toBeInTheDocument();
    });
  });

  it('should show loading state during authentication', async () => {
    const { apiClient } = await import('./lib/api-client');
    
    vi.mocked(apiClient.getDevToken).mockImplementation(
      () => new Promise((resolve) => setTimeout(() => resolve('mock-token'), 100))
    );

    renderWithProviders(<App />);

    expect(screen.getByText(/connecting to api/i)).toBeInTheDocument();
  });

  it('should handle authentication failure', async () => {
    const { apiClient } = await import('./lib/api-client');
    
    vi.mocked(apiClient.getDevToken).mockRejectedValueOnce(
      new Error('Cannot connect to API')
    );

    renderWithProviders(<App />);

    await waitFor(() => {
      expect(screen.getByText(/failed to authenticate/i)).toBeInTheDocument();
      expect(screen.getByText(/cannot connect to api/i)).toBeInTheDocument();
    });
  });

  it('should display navigation with all section links', async () => {
    renderWithProviders(<App />);

    await waitFor(() => {
      expect(screen.getByText('CurrencyX')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /converter/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /live rates/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /historical/i })).toBeInTheDocument();
    });
  });

  it('should display hero section with title', async () => {
    renderWithProviders(<App />);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /currency conversion/i })).toBeInTheDocument();
      expect(screen.getByText(/made simple/i)).toBeInTheDocument();
    });
  });

  it('should show retry button on authentication error', async () => {
    const { apiClient } = await import('./lib/api-client');
    
    vi.mocked(apiClient.getDevToken).mockRejectedValueOnce(new Error('Network error'));

    renderWithProviders(<App />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
    });
  });

  it('should display troubleshooting information on auth error', async () => {
    const { apiClient } = await import('./lib/api-client');
    
    vi.mocked(apiClient.getDevToken).mockRejectedValueOnce(new Error('Connection failed'));

    renderWithProviders(<App />);

    await waitFor(() => {
      expect(screen.getByText(/troubleshooting/i)).toBeInTheDocument();
      expect(screen.getByText(/make sure the backend api is running/i)).toBeInTheDocument();
    });
  });
});
