import { useApiStore } from '@/stores/apiStore';

export function useStepOutPlanService() {
    const api = useApiStore().api;

    async function listStepOutPlans() {
        const { data } = await api.get('/api/v1/planning/step-out-plans');
        return data;
    }
    async function getStepOutPlan(id: number) {
        const { data } = await api.get(`/api/v1/planning/step-out-plans/${id}`);
        return data;
    }
    async function createStepOutPlan(payload: Record<string, unknown>) {
        const { data } = await api.post('/api/v1/planning/step-out-plans', payload);
        return data;
    }
    async function updateStepOutPlan(id: number, payload: Record<string, unknown>) {
        const { data } = await api.put(`/api/v1/planning/step-out-plans/${id}`, payload);
        return data;
    }
    async function deleteStepOutPlan(id: number) {
        await api.delete(`/api/v1/planning/step-out-plans/${id}`);
    }
    async function createStep(planId: number, payload: Record<string, unknown>) {
        const { data } = await api.post(`/api/v1/planning/step-out-plans/${planId}/steps`, payload);
        return data;
    }
    async function updateStep(planId: number, stepId: number, payload: Record<string, unknown>) {
        const { data } = await api.put(`/api/v1/planning/step-out-plans/${planId}/steps/${stepId}`, payload);
        return data;
    }
    async function deleteStep(planId: number, stepId: number) {
        await api.delete(`/api/v1/planning/step-out-plans/${planId}/steps/${stepId}`);
    }
    async function generateWorkPackages(planId: number) {
        const { data } = await api.post(`/api/v1/planning/step-out-plans/${planId}/generate-work-packages`);
        return data;
    }

    return { listStepOutPlans, getStepOutPlan, createStepOutPlan, updateStepOutPlan, deleteStepOutPlan, createStep, updateStep, deleteStep, generateWorkPackages };
}
