import { useApiStore } from '@/stores/apiStore';

export function useSchedulingService() {
  const apiStore = useApiStore();
  const api = apiStore.api;

  // ── Dashboard ─────────────────────────────────────────────────────────────
  async function getDashboard() {
    const { data } = await api.get('/api/v1/scheduling/dashboard');
    return data;
  }

  // ── Jobs (demand sources) ─────────────────────────────────────────────────
  async function listJobs() {
    const { data } = await api.get('/api/v1/scheduling/jobs');
    return data;
  }

  // ── Resources ─────────────────────────────────────────────────────────────
  async function listResources(params?: {
    craft?: string;
    active?: boolean;
    region?: string;
    branch?: string;
    status?: string;
  }) {
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

  // ── Certifications ────────────────────────────────────────────────────────
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

  // ── Assignments ───────────────────────────────────────────────────────────
  async function listAssignments(params?: {
    resourceId?: number;
    jobSourceType?: string;
    jobSourceId?: number;
  }) {
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

  // ── Coverage ──────────────────────────────────────────────────────────────
  async function getCoverage() {
    const { data } = await api.get('/api/v1/scheduling/coverage');
    return data;
  }

  async function getSuggestedMatches() {
    const { data } = await api.get('/api/v1/scheduling/suggested-matches');
    return data;
  }

  async function getCraftDetail(craftCode: string) {
    const { data } = await api.get(`/api/v1/scheduling/coverage/craft/${craftCode}/detail`);
    return data;
  }

  async function getCraftTrend(craftCode: string, days = 30, includeForecast = false) {
    const { data } = await api.get(`/api/v1/scheduling/coverage/craft/${craftCode}/trend`, {
      params: { days, includeForecast },
    });
    return data;
  }

  // ── Roll-off / Ending Soon ────────────────────────────────────────────────
  async function getEndingSoon(days = 14) {
    const { data } = await api.get('/api/v1/scheduling/ending-soon', { params: { days } });
    return data;
  }

  return {
    getDashboard,
    listJobs,
    listResources, getResource, createResource, updateResource, deleteResource,
    listCertifications, createCertification, deleteCertification,
    listAssignments, createAssignment, updateAssignment, deleteAssignment,
    getCoverage, getSuggestedMatches, getCraftDetail, getCraftTrend,
    getEndingSoon,
  };
}
