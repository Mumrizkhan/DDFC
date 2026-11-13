import api from './api';

export interface Laboratory {
  id: string;
  name: string;
  email: string | null;
  phone: string | null;
  isActive: boolean;
}

export interface SoilTestSchedule {
  laboratoryId: string;
  laboratoryName: string;
  scheduledAt: string;
  location: string | null;
  notes: string | null;
}

export const soilTestService = {
  labs: (includeInactive = false) => api.get<Laboratory[]>('/laboratories', { params: { includeInactive } }).then((response) => response.data),
  createLab: (data: Omit<Laboratory, 'id'>) => api.post<Laboratory>('/laboratories', data).then((response) => response.data),
  updateLab: (id: string, data: Omit<Laboratory, 'id'>) => api.put<Laboratory>(`/laboratories/${id}`, data).then((response) => response.data),
  schedule: (requestId: string) => api.get<SoilTestSchedule | null>(`/requests/${requestId}/soil-test/schedule`).then((response) => response.data || null),
  saveSchedule: (requestId: string, data: { laboratoryId: string; scheduledAt: string; location?: string; notes?: string }) =>
    api.put<{ schedule: SoilTestSchedule; notificationWarning: boolean; emailWarning: boolean }>(`/requests/${requestId}/soil-test/schedule`, data).then((response) => response.data),
};