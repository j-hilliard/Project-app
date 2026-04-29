import { useApiStore } from '@/stores/apiStore';

export function useFcoService() {
    const api = useApiStore().api;

    async function listFcos(params?: { workOrderId?: number; status?: string }) {
        const { data } = await api.get('/api/v1/planning/fco', { params });
        return data;
    }
    async function createFco(payload: Record<string, unknown>) {
        const { data } = await api.post('/api/v1/planning/fco', payload);
        return data;
    }
    async function updateFco(id: number, payload: Record<string, unknown>) {
        const { data } = await api.put(`/api/v1/planning/fco/${id}`, payload);
        return data;
    }
    async function generateFcoDocument(id: number): Promise<Blob> {
        const { data } = await api.post(
            `/api/v1/planning/fco/${id}/generate-document`,
            null,
            { responseType: 'blob' }
        );
        return data;
    }

    return { listFcos, createFco, updateFco, generateFcoDocument };
}
