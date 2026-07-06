export const DEFAULT_API_BASE_URL = 'http://localhost:5001/api/v1';

function trimTrailingSlash(value: string) {
  return value.replace(/\/+$/, '');
}

export const appConfig = {
  apiBaseUrl: trimTrailingSlash(process.env.NEXT_PUBLIC_API_BASE_URL || process.env.NEXT_PUBLIC_API_URL || DEFAULT_API_BASE_URL),
};
