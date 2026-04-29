<template>
    <span>{{ formatted }}</span>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useFormatters } from '../composables/useFormatters';

const props = defineProps<{ value?: number | null }>();
const { fmtCurrency } = useFormatters();
const formatted = computed(() => fmtCurrency(props.value));
</script>
