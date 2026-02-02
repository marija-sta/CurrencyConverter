import axios from 'axios';
import type { AxiosInstance } from 'axios';

class ApiClient {
  private client: AxiosInstance;
  private token: string | null = null;

  constructor() {
    this.client = axios.create({
      baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5014/api/v1',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.client.interceptors.request.use((config) => {
      if (this.token) {
        config.headers.Authorization = `Bearer ${this.token}`;
      }
      return config;
    });
  }

  setToken(token: string) {
    this.token = token;
  }

  getToken() {
    return this.token;
  }

  clearToken() {
    this.token = null;
  }

  // DEV ONLY: Fetches JWT token from development endpoint
  // This endpoint only exists in Development environment
  // Production should use proper OAuth/authentication flow
  async getDevToken(clientId: string = 'test-user', roles: string[] = ['admin']): Promise<string> {
    try {
      // Using absolute URL because dev token endpoint is not under /api/v1
      const devTokenUrl = 'http://localhost:5014/api/dev/token';
      console.log('Fetching dev token from:', devTokenUrl);
      
      const response = await axios.post(devTokenUrl, {
        clientId,
        roles,
      });
      
      const token = response.data.token;
      console.log('Dev token received successfully');
      this.setToken(token);
      return token;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        console.error('Failed to get dev token:', {
          message: error.message,
          status: error.response?.status,
          statusText: error.response?.statusText,
          data: error.response?.data,
          url: error.config?.url,
        });
        
        // Provide helpful error message
        if (error.code === 'ERR_NETWORK' || error.message.includes('Network Error')) {
          throw new Error('Cannot connect to API. Make sure backend is running at http://localhost:5014');
        }
        
        if (error.response?.status === 404) {
          throw new Error('Dev token endpoint not found. Make sure you are running in Development mode.');
        }
        
        throw new Error(`Authentication failed: ${error.response?.data?.message || error.message}`);
      }
      throw error;
    }
  }

  async convert(amount: number, from: string, to: string) {
    const response = await this.client.get('/convert', {
      params: { amount, from, to },
    });
    return response.data;
  }

  async getLatestRates(baseCurrency: string) {
    const response = await this.client.get('/rates/latest', {
      params: { baseCurrency },
    });
    return response.data;
  }

  async getHistoricalRates(
    baseCurrency: string,
    start: string,
    end: string,
    page: number = 1,
    pageSize: number = 10
  ) {
    const response = await this.client.get('/rates/historical', {
      params: { baseCurrency, start, end, page, pageSize },
    });
    return response.data;
  }
}

export const apiClient = new ApiClient();
