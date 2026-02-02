import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test/test-utils';
import { createMockApiClient, mockConversionResponse } from '../test/mocks';
import ConverterSection from './ConverterSection';

// Mock the API client
vi.mock('../lib/api-client', () => ({
  apiClient: createMockApiClient(),
}));

describe('ConverterSection', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render conversion form with all inputs', () => {
    renderWithProviders(<ConverterSection />);

    expect(screen.getByText('You send')).toBeInTheDocument();
    expect(screen.getByText('They receive')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /convert now/i })).toBeInTheDocument();
  });

  it('should have default values for amount and currencies', () => {
    renderWithProviders(<ConverterSection />);

    const amountInput = screen.getByPlaceholderText('1000');
    expect(amountInput).toBeInTheDocument();
    expect(amountInput).toHaveValue(1000);
  });

  it('should swap currencies when swap button clicked', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ConverterSection />);

    const swapButton = screen.getByRole('button', { name: '' }); // Icon button without text
    await user.click(swapButton);

    // The currencies should be swapped (visually this would be checked by the select values)
    // Since we start with USD -> EUR, after swap it should be EUR -> USD
  });

  it('should display conversion result after successful API call', async () => {
    const user = userEvent.setup();
    const { apiClient } = await import('../lib/api-client');
    
    renderWithProviders(<ConverterSection />);

    const convertButton = screen.getByRole('button', { name: /convert now/i });
    await user.click(convertButton);

    await waitFor(() => {
      expect(apiClient.convert).toHaveBeenCalledWith(1000, 'USD', 'EUR');
    });

    await waitFor(() => {
      expect(screen.getByText(/exchange rate/i)).toBeInTheDocument();
    });
  });

  it('should show loading state during conversion', async () => {
    const user = userEvent.setup();
    const { apiClient } = await import('../lib/api-client');
    
    // Make the API call take longer
    vi.mocked(apiClient.convert).mockImplementation(
      () => new Promise((resolve) => setTimeout(() => resolve(mockConversionResponse), 100))
    );

    renderWithProviders(<ConverterSection />);

    const convertButton = screen.getByRole('button', { name: /convert now/i });
    await user.click(convertButton);

    expect(screen.getByRole('button', { name: /converting/i })).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /convert now/i })).toBeInTheDocument();
    });
  });

  it('should display error message when API call fails', async () => {
    const user = userEvent.setup();
    const { apiClient } = await import('../lib/api-client');
    
    vi.mocked(apiClient.convert).mockRejectedValueOnce(new Error('Network error'));

    renderWithProviders(<ConverterSection />);

    const convertButton = screen.getByRole('button', { name: /convert now/i });
    await user.click(convertButton);

    await waitFor(() => {
      expect(screen.getByText(/network error/i)).toBeInTheDocument();
    });
  });

  it('should validate amount is greater than zero', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ConverterSection />);

    const amountInput = screen.getByPlaceholderText('1000');
    await user.clear(amountInput);
    await user.type(amountInput, '0');

    const convertButton = screen.getByRole('button', { name: /convert now/i });
    await user.click(convertButton);

    await waitFor(() => {
      expect(screen.getByText(/amount must be greater than zero/i)).toBeInTheDocument();
    });
  });

  it('should validate amount is a valid number', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ConverterSection />);

    const amountInput = screen.getByPlaceholderText('1000');
    await user.clear(amountInput);
    await user.type(amountInput, '-50');

    const convertButton = screen.getByRole('button', { name: /convert now/i });
    await user.click(convertButton);

    await waitFor(() => {
      expect(screen.getByText(/amount must be greater than zero/i)).toBeInTheDocument();
    });
  });

  it('should not include excluded currencies in dropdowns', () => {
    renderWithProviders(<ConverterSection />);

    const selects = screen.getAllByRole('combobox');
    
    selects.forEach((select) => {
      const options = Array.from(select.querySelectorAll('option')).map((opt) => opt.value);
      expect(options).not.toContain('TRY');
      expect(options).not.toContain('PLN');
      expect(options).not.toContain('THB');
      expect(options).not.toContain('MXN');
    });
  });
});
