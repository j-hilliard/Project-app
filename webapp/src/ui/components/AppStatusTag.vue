<template>
    <Tag :value="value" :severity="severity" />
</template>

<script setup lang="ts">
import Tag from 'primevue/tag';

const props = defineProps<{ value: string; severity?: string }>();
</script>
