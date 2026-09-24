import { apiClient } from './client';
import type { AttributeCategory, AttributeDefinition, AuthResponse, Cv, CvDocument, DiscussionPost, Profile, ProfileEditor, SaveAttributeDefinition, SavePosition, Position, PositionListItem, Project, SearchResult, Stats, AttributeValueInput } from './types';

export const accountApi = { register: async (email: string, password: string) => (await apiClient.post<AuthResponse>('/api/account/register', { email, password })).data, login: async (email: string, password: string) => (await apiClient.post<AuthResponse>('/api/account/login', { email, password })).data, me: async () => (await apiClient.get<Profile>('/api/account/me')).data, logout: async () => { await apiClient.post('/api/account/logout'); } };
export const attributesApi = {
  search: async (namePrefix = '', categoryId = '') => (await apiClient.get<AttributeDefinition[]>('/api/attributes', { params: { namePrefix: namePrefix || undefined, categoryId: categoryId || undefined } })).data,
  categories: async () => (await apiClient.get<AttributeCategory[]>('/api/attributes/categories')).data,
  create: async (body: SaveAttributeDefinition) => (await apiClient.post<AttributeDefinition>('/api/attributes', body)).data,
  update: async (id: string, body: SaveAttributeDefinition) => (await apiClient.put<AttributeDefinition>(`/api/attributes/${id}`, body)).data,
  remove: async (ids: string[]) => { await apiClient.delete('/api/attributes', { data: { ids } }); },
};
export const positionsApi = {
  list: async () => (await apiClient.get<PositionListItem[]>('/api/positions')).data,
  latest: async () => (await apiClient.get<PositionListItem[]>('/api/positions/latest', { params: { count: 5 } })).data,
  popular: async () => (await apiClient.get<PositionListItem[]>('/api/positions/popular', { params: { count: 5 } })).data,
  get: async (id: string) => (await apiClient.get<Position>(`/api/positions/${id}`)).data,
  create: async (body: SavePosition) => (await apiClient.post<{ id: string }>('/api/positions', body)).data,
  update: async (id: string, body: SavePosition) => (await apiClient.put(`/api/positions/${id}`, body)).data,
  remove: async (ids: string[]) => { await apiClient.delete('/api/positions', { data: { ids } }); },
  duplicate: async (id: string) => (await apiClient.post<{ id: string }>(`/api/positions/${id}/duplicate`)).data,
};
export const profilesApi = {
  editor: async (id: string) => (await apiClient.get<ProfileEditor>(`/api/profiles/${id}/edit`)).data,
  updateProject: async (id: string, projectId: string, body: Omit<Project, 'id'>) => (await apiClient.put<Project>(`/api/profiles/${id}/projects/${projectId}`, body)).data,
  tagSuggestions: async (prefix: string) => (await apiClient.get<string[]>('/api/tags/suggestions', { params: { prefix } })).data,
  me: async () => (await apiClient.get<ProfileEditor>('/api/profiles/me')).data,
  autosave: async (id: string, version: number, changes: { attributeDefinitionId: string; value: AttributeValueInput }[], removedAttributeIds: string[] = []) => (await apiClient.put<{ version: number }>(`/api/profiles/${id}/autosave`, { version, changes, removedAttributeIds })).data,
  addProject: async (id: string, body: Omit<Project, 'id'>) => (await apiClient.post<Project>(`/api/profiles/${id}/projects`, body)).data,
  removeProjects: async (id: string, ids: string[]) => { await apiClient.delete(`/api/profiles/${id}/projects`, { data: { ids } }); },
};
export const cvsApi = { mine: async () => (await apiClient.get<Cv[]>('/api/cvs/me')).data, get: async (id: string) => (await apiClient.get<CvDocument>(`/api/cvs/${id}`)).data, generate: async (positionId: string) => (await apiClient.post<{ id: string }>(`/api/cvs/generate/${positionId}`)).data, setAttribute: async (id: string, version: number, attributeDefinitionId: string, value: AttributeValueInput) => (await apiClient.put<{ version: number }>(`/api/cvs/${id}/attributes`, { version, changes: [{ attributeDefinitionId, value }], removedAttributeIds: [] })).data, publish: async (id: string) => { await apiClient.post(`/api/cvs/${id}/publish`); }, unpublish: async (id: string) => { await apiClient.post(`/api/cvs/${id}/unpublish`); }, remove: async (ids: string[]) => { await apiClient.delete('/api/cvs', { data: { ids } }); } };
export const discussionsApi = { byPosition: async (positionId: string) => (await apiClient.get<DiscussionPost[]>(`/api/discussions/position/${positionId}`)).data, add: async (positionId: string, content: string) => (await apiClient.post<DiscussionPost>(`/api/discussions/position/${positionId}`, { content })).data };
export const likesApi = { toggle: async (cvId: string) => (await apiClient.post<{ cvId: string; likeCount: number; likedByMe: boolean }>(`/api/likes/${cvId}/toggle`)).data };
export const discoveryApi = { search: async (q: string) => (await apiClient.get<SearchResult>('/api/search', { params: { q } })).data, stats: async () => (await apiClient.get<Stats>('/api/stats')).data, tags: async () => (await apiClient.get<{tag:string;count:number}[]>('/api/tags')).data };
