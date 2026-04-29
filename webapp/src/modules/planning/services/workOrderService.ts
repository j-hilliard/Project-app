import { useApiStore } from '@/stores/apiStore';

export function useWorkOrderService() {
    const api = useApiStore().api;

    async function listWorkOrders(params?: { projectId?: number; estimateId?: number; status?: string }) {
        const { data } = await api.get('/api/v1/work-orders', { params });
        return data;
    }
    async function getWorkOrder(id: number) {
        const { data } = await api.get(`/api/v1/work-orders/${id}`);
        return data;
    }
    async function getWorkOrderFinancials(id: number) {
        const { data } = await api.get(`/api/v1/work-orders/${id}/financials`);
        return data;
    }
    async function releaseWorkOrder(id: number) {
        const { data } = await api.patch(`/api/v1/work-orders/${id}/release`);
        return data;
    }
    async function setWorkOrderStatus(id: number, status: string) {
        const { data } = await api.patch(`/api/v1/work-orders/${id}/status`, { status });
        return data;
    }

    return { listWorkOrders, getWorkOrder, getWorkOrderFinancials, releaseWorkOrder, setWorkOrderStatus };
}
