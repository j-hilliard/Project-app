<template>
    <span>{{ formatted }}</span>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useFormatters } from '../composables/useFormatters';

const props = defineProps<{ value?: string | null }>();
const { fmtDate } = useFormatters();
const formatted = computed(() => fmtDate(props.value));
</script>
