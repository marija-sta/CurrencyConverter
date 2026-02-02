import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useState, useEffect } from 'react';
import { RefreshCw, Activity, Clock, Download } from 'lucide-react';
import { apiClient } from './lib/api-client';
import ConverterSection from './components/ConverterSection';
import LiveRatesSection from './components/LiveRatesSection';
import HistoricalSection from './components/HistoricalSection';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

type Section = 'converter' | 'live' | 'historical';

function App() {
  const [activeSection, setActiveSection] = useState<Section>('converter');
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [authError, setAuthError] = useState<string | null>(null);

  useEffect(() => {
    const initAuth = async () => {
      try {
        // DEV ONLY: Auto-fetch JWT token for testing
        // Production: Replace with proper login flow
        console.log('Initializing authentication...');
        await apiClient.getDevToken('demo-user', ['admin']);
        console.log('Authentication successful');
        setIsAuthenticated(true);
      } catch (error) {
        console.error('Failed to authenticate:', error);
        const errorMessage = error instanceof Error ? error.message : 'Unknown authentication error';
        setAuthError(errorMessage);
      } finally {
        setIsLoading(false);
      }
    };

    initAuth();
  }, []);

  const scrollToSection = (section: Section) => {
    setActiveSection(section);
    const element = document.getElementById(section);
    element?.scrollIntoView({ behavior: 'smooth' });
  };

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-teal-500 mb-4"></div>
          <div className="text-white text-xl">Connecting to API...</div>
          <div className="text-white/60 text-sm mt-2">
            Fetching authentication token from backend
          </div>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-white rounded-lg shadow-lg p-6">
          <div className="text-red-600 mb-4">
            <svg className="w-12 h-12 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <h2 className="text-xl font-bold text-center mb-2">Failed to authenticate</h2>
          </div>
          <div className="text-gray-700 mb-4">
            <p className="mb-2"><strong>Error:</strong> {authError}</p>
            <div className="bg-gray-50 p-4 rounded mt-4 text-sm">
              <p className="font-semibold mb-2">Troubleshooting:</p>
              <ol className="list-decimal list-inside space-y-1">
                <li>Make sure the backend API is running</li>
                <li>Check it's accessible at: <code className="bg-gray-200 px-1 rounded">http://localhost:5014</code></li>
                <li>Verify the dev token endpoint exists: <code className="bg-gray-200 px-1 rounded">/api/dev/token</code></li>
                <li>Check backend console for any errors</li>
              </ol>
            </div>
          </div>
          <button
            onClick={() => window.location.reload()}
            className="w-full bg-teal-500 text-white py-2 px-4 rounded hover:bg-teal-600 transition-colors"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <QueryClientProvider client={queryClient}>
      <div className="min-h-screen">
        {/* Navigation */}
        <nav className="fixed top-0 left-0 right-0 z-50 bg-[#1e3a5f]/80 backdrop-blur-sm border-b border-white/10">
          <div className="container mx-auto px-4 py-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <div className="w-10 h-10 bg-gradient-to-br from-teal-400 to-teal-600 rounded-lg flex items-center justify-center">
                  <RefreshCw className="w-6 h-6 text-white" />
                </div>
                <span className="text-white text-xl font-bold">CurrencyX</span>
              </div>

              <div className="flex items-center gap-6">
                <button
                  onClick={() => scrollToSection('converter')}
                  className={`text-sm font-medium transition-colors ${
                    activeSection === 'converter'
                      ? 'text-white'
                      : 'text-white/70 hover:text-white'
                  }`}
                >
                  Converter
                </button>
                <button
                  onClick={() => scrollToSection('live')}
                  className={`text-sm font-medium transition-colors ${
                    activeSection === 'live'
                      ? 'text-white'
                      : 'text-white/70 hover:text-white'
                  }`}
                >
                  Live Rates
                </button>
                <button
                  onClick={() => scrollToSection('historical')}
                  className={`text-sm font-medium transition-colors ${
                    activeSection === 'historical'
                      ? 'text-white'
                      : 'text-white/70 hover:text-white'
                  }`}
                >
                  Historical
                </button>
                <button className="text-white/70 hover:text-white transition-colors">
                  <Download className="w-5 h-5" />
                </button>
              </div>
            </div>
          </div>
        </nav>

        {/* Hero Section */}
        <section className="pt-32 pb-20 container mx-auto px-4">
          <div className="text-center mb-12">
            <div className="inline-flex items-center gap-2 bg-white/10 backdrop-blur-sm rounded-full px-4 py-2 mb-6">
              <Activity className="w-4 h-4 text-teal-400" />
              <span className="text-white/90 text-sm">Real-time exchange rates powered by Frankfurter API</span>
            </div>
            
            <h1 className="text-5xl md:text-6xl font-bold text-white mb-4">
              Currency Conversion
              <br />
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-teal-400 to-cyan-400">
                Made Simple
              </span>
            </h1>
            
            <p className="text-white/70 text-lg max-w-2xl mx-auto">
              Fast, reliable, and secure currency conversion with live rates. Built for accuracy and trusted by thousands worldwide.
            </p>
          </div>

          <div className="flex items-center justify-center gap-8 mb-12">
            <div className="flex items-center gap-2 text-white/80">
              <div className="w-8 h-8 rounded-full bg-white/10 flex items-center justify-center">
                <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clipRule="evenodd" />
                </svg>
              </div>
              <span className="text-sm">Secure API</span>
            </div>
            <div className="flex items-center gap-2 text-white/80">
              <div className="w-8 h-8 rounded-full bg-white/10 flex items-center justify-center">
                <Activity className="w-4 h-4" />
              </div>
              <span className="text-sm">Real-time Rates</span>
            </div>
            <div className="flex items-center gap-2 text-white/80">
              <div className="w-8 h-8 rounded-full bg-white/10 flex items-center justify-center">
                <Clock className="w-4 h-4" />
              </div>
              <span className="text-sm">Historical Data</span>
            </div>
          </div>

          {/* Converter Section */}
          <div id="converter">
            <ConverterSection />
          </div>
        </section>

        {/* Live Rates Section */}
        <section id="live" className="py-20 bg-white/5 backdrop-blur-sm">
          <div className="container mx-auto px-4">
            <LiveRatesSection />
          </div>
        </section>

        {/* Historical Section */}
        <section id="historical" className="py-20">
          <div className="container mx-auto px-4">
            <HistoricalSection />
          </div>
        </section>
      </div>
    </QueryClientProvider>
  );
}

export default App;
