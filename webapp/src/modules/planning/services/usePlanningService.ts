import { useApiStore } from '@/stores/apiStore';

export function usePlanningService() {
  const apiStore = useApiStore();
  const api = apiStore.api;

  // ── Projects ──────────────────────────────────────────────────────────────
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

  // ── Work Orders ───────────────────────────────────────────────────────────
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

  // ── Step-Out Plans ────────────────────────────────────────────────────────
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

  // ── Work Packages ─────────────────────────────────────────────────────────
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

  // ── FCO Documents ─────────────────────────────────────────────────────────
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

  return {
    listProjects, getProject, getProjectFull, setProjectStatus, lockProjectBaseline,
    listWorkOrders, getWorkOrder, getWorkOrderFinancials, releaseWorkOrder, setWorkOrderStatus,
    listStepOutPlans, getStepOutPlan, createStepOutPlan, updateStepOutPlan, deleteStepOutPlan,
    createStep, updateStep, deleteStep, generateWorkPackages,
    listWorkPackages, getWorkPackage, updateWorkPackage,
    listFcos, createFco, updateFco, generateFcoDocument,
  };
}
