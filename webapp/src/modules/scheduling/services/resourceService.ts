import { useApiStore } from '@/stores/apiStore';

export function useResourceService() {
    const api = useApiStore().api;

    async function listResources(params?: { craft?: string; active?: boolean; region?: string; branch?: string; status?: string }) {
        const { data } = await api.get('/api/v1/scheduling/resources', { params });
        return data;
    }
    async function getResource(id: number) {
        const { data } = await api.get(`/api/v1/scheduling/resources/${id}`);
        return data;
    }
    async function createResource(payload: Record<string, unknown>) {
        const { data } = await api.post('/api/v1/scheduling/resources', payload);
        return data;
    }
    async function updateResource(id: number, payload: Record<string, unknown>) {
        const { data } = await api.put(`/api/v1/scheduling/resources/${id}`, payload);
        return data;
    }
    async function deleteResource(id: number) {
        await api.delete(`/api/v1/scheduling/resources/${id}`);
    }
    async function listCertifications(resourceId: number) {
        const { data } = await api.get(`/api/v1/scheduling/resources/${resourceId}/certifications`);
        return data;
    }
    async function createCertification(payload: { resourceId: number; type: string; expirationDate?: string | null }) {
        const { data } = await api.post('/api/v1/scheduling/certifications', payload);
        return data;
    }
    async function deleteCertification(id: number) {
        await api.delete(`/api/v1/scheduling/certifications/${id}`);
    }

    return { listResources, getResource, createResource, updateResource, deleteResource, listCertifications, createCertification, deleteCertification };
}
