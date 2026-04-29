import { useApiStore } from '@/stores/apiStore';

export function useCoverageService() {
    const api = useApiStore().api;

    async function getDashboard() {
        const { data } = await api.get('/api/v1/scheduling/dashboard');
        return data;
    }
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
    async function getEndingSoon(days = 14) {
        const { data } = await api.get('/api/v1/scheduling/ending-soon', { params: { days } });
        return data;
    }

    return { getDashboard, getCoverage, getSuggestedMatches, getCraftDetail, getCraftTrend, getEndingSoon };
}
