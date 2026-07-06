export const DEFAULT_API_BASE_URL = 'http://localhost:5001/api/v1';
export const DEFAULT_SWAGGER_URL = 'http://localhost:5001/swagger/index.html';

function trimTrailingSlash(value: string) {
  return value.replace(/\/+$/, '');
}

export const appConfig = {
  apiBaseUrl: trimTrailingSlash(import.meta.env.VITE_API_BASE_URL || import.meta.env.VITE_API_URL || DEFAULT_API_BASE_URL),
  apiSwaggerUrl: import.meta.env.VITE_SWAGGER_URL || DEFAULT_SWAGGER_URL,
  defaultAdminEmail: import.meta.env.DEV ? (import.meta.env.VITE_DEFAULT_ADMIN_EMAIL ?? '') : '',
  defaultAdminPassword: import.meta.env.DEV ? (import.meta.env.VITE_DEFAULT_ADMIN_PASSWORD ?? '') : '',
};
