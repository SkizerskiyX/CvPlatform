const base = import.meta.env.VITE_API_URL ?? 'https://localhost:7000';
export type Position = { id: string; name: string; description?: string };
export type Cv = { id: string; profileId: string; title: string; status: string };
export type CvAttributeValue = { attributeDefinitionId: string; value?: string };
export type CvDetails = Cv & { attributeValues: CvAttributeValue[] };
export type Category = { id: string; name: string; description?: string };
export type AttributeItem = { id: string; categoryId: string; categoryName: string; key: string; name: string; dataType: string; isRequired: boolean };
export type Profile = { id: string; userId: string; displayName: string; bio?: string };
export type Discussion = { id: string; cvId: string; authorUserId: string; content: string; status: string; likeCount: number; likedByCurrentUser: boolean };

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const method = (init.method ?? 'GET').toUpperCase();
  const headers = new Headers(init.headers);
  headers.set('Content-Type', 'application/json');
  if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(method)) {
    const token = document.cookie.split('; ').find(value => value.startsWith('XSRF-TOKEN='))?.split('=').slice(1).join('=');
    if (token) headers.set('X-XSRF-TOKEN', decodeURIComponent(token));
  }
  const response = await fetch(`${base}${path}`, { ...init, credentials: 'include', headers });
  if (response.status === 401) throw new Error('AUTH_REQUIRED');
  if (response.status === 403) throw new Error('FORBIDDEN');
  if (!response.ok) throw new Error((await response.json().catch(() => null))?.message ?? 'Request failed');
  return response.status === 204 ? undefined as T : response.json();
}

export const api = {
  csrf: () => request<void>('/api/account/csrf'),
  me: () => request<{ authenticated: boolean; userId?: string; email?: string }>('/api/account/me'),
  login: (body: { email: string; password: string; rememberMe: boolean }) => request('/api/account/login', { method: 'POST', body: JSON.stringify(body) }),
  register: (body: { email: string; password: string; rememberMe: boolean }) => request('/api/account/register', { method: 'POST', body: JSON.stringify(body) }),
  logout: () => request<void>('/api/account/logout', { method: 'POST' }),
  positions: () => request<Position[]>('/api/positions'),
  position: (id: string) => request<Position>(`/api/positions/${id}`),
  cvs: () => request<Cv[]>('/api/cvs'),
  cvDetails: (id: string) => request<CvDetails>(`/api/cvs/${id}/details`),
  createCv: (body: { profileId: string; title: string }) => request<Cv>('/api/cvs', { method: 'POST', body: JSON.stringify(body) }),
  createCvForUser: (body: { title: string }) => request<Cv>('/api/cvs/me', { method: 'POST', body: JSON.stringify(body) }),
  updateProfile: (body: { displayName: string; bio?: string }) => request<void>('/api/profiles/me', { method: 'PUT', body: JSON.stringify(body) }),
  publishCv: (id: string) => request<void>(`/api/cvs/${id}/publish`, { method: 'POST' }),
  setCvAttribute: (id: string, body: { attributeDefinitionId: string; value?: string }) => request<void>(`/api/cvs/${id}/attributes`, { method: 'PUT', body: JSON.stringify(body) }),
  categories: () => request<Category[]>('/api/attributes/categories'),
  attributes: () => request<AttributeItem[]>('/api/attributes'),
  myProfile: () => request<Profile>('/api/profiles/me'),
  discussions: (cvId: string) => request<Discussion[]>(`/api/discussions/cv/${cvId}`),
  addDiscussion: (body: { cvId: string; content: string }) => request<Discussion>('/api/discussions', { method: 'POST', body: JSON.stringify(body) }),
  like: (id: string) => request<{ liked: boolean }>(`/api/discussions/${id}/like`, { method: 'POST' })
};
