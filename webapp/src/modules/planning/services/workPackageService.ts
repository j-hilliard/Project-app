import { useApiStore } from '@/stores/apiStore';

export function useWorkPackageService() {
    const api = useApiStore().api;

    async function listWorkPackages(params?: { planId?: number; workOrderId?: number }) {
        const { data } = await api.get('/api/v1/planning/work-packages', { params });
        return data;
    }
    async function getWorkPackage(id: number) {
        const { data } = await api.get(`/api/v1/planning/work-packages/${id}`);
        return data;
    }
    async function updateWorkPackage(id: number, payload: Record<string, unknown>) {
        const { data } = await api.put(`/api/v1/planning/work-packages/${id}`, payload);
        return data;
    }

    return { listWorkPackages, getWorkPackage, updateWorkPackage };
}
