import { useApiStore } from '@/stores/apiStore';

export function useAssignmentService() {
    const api = useApiStore().api;

    async function listAssignments(params?: { resourceId?: number; jobSourceType?: string; jobSourceId?: number }) {
        const { data } = await api.get('/api/v1/scheduling/assignments', { params });
        return data;
    }
    async function createAssignment(payload: Record<string, unknown>) {
        const { data } = await api.post('/api/v1/scheduling/assignments', payload);
        return data;
    }
    async function updateAssignment(id: number, payload: Record<string, unknown>) {
        const { data } = await api.put(`/api/v1/scheduling/assignments/${id}`, payload);
        return data;
    }
    async function deleteAssignment(id: number) {
        await api.delete(`/api/v1/scheduling/assignments/${id}`);
    }
    async function listJobs() {
        const { data } = await api.get('/api/v1/scheduling/jobs');
        return data;
    }

    return { listAssignments, createAssignment, updateAssignment, deleteAssignment, listJobs };
}
