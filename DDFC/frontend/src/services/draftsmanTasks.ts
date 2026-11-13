import api from './api';

export type DraftsmanParent = 'Architecture' | 'ThreeD' | 'Structure' | 'MEP';

export interface DraftsmanTask {
  id: string;
  requestId: string;
  requestNumber: string;
  parentStep: DraftsmanParent;
  assignedToUserId: string;
  assignedToUserName: string;
  assignedAt: string;
  fileUrl: string | null;
  completedAt: string | null;
}

export const draftsmanTasksService = {
  mine: () => api.get<DraftsmanTask[]>('/draftsman-tasks/my').then((response) => response.data),
  forRequest: (requestId: string) => api.get<DraftsmanTask[]>(`/draftsman-tasks/request/${requestId}`).then((response) => response.data),
  get: (id: string) => api.get<DraftsmanTask>(`/draftsman-tasks/${id}`).then((response) => response.data),
  assign: (requestId: string, parentStep: DraftsmanParent, assignedToUserId: string) =>
    api.post<DraftsmanTask>('/draftsman-tasks', { requestId, parentStep, assignedToUserId }).then((response) => response.data),
  complete: (id: string, fileUrl: string) =>
    api.post<DraftsmanTask>(`/draftsman-tasks/${id}/complete`, { fileUrl }).then((response) => response.data),
};