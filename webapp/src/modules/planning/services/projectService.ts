import { useApiStore } from '@/stores/apiStore';

export function useProjectService() {
    const api = useApiStore().api;

    async function listProjects(params?: { status?: string; estimateId?: number }) {
        const { data } = await api.get('/api/v1/projects', { params });
        return data;
    }
    async function getProject(id: number) {
        const { data } = await api.get(`/api/v1/projects/${id}`);
        return data;
    }
    async function getProjectFull(id: number) {
        const { data } = await api.get(`/api/v1/projects/${id}/full`);
        return data;
    }
    async function setProjectStatus(id: number, status: string) {
        const { data } = await api.patch(`/api/v1/projects/${id}/status`, { status });
        return data;
    }
    async function lockProjectBaseline(id: number, label?: string | null, reason?: string | null) {
        const { data } = await api.post(`/api/v1/projects/${id}/baseline`, { label, reason });
        return data;
    }

    return { listProjects, getProject, getProjectFull, setProjectStatus, lockProjectBaseline };
}
