<template>
    <div class="module-view">
        <slot />
    </div>
</template>

<style scoped>
.module-view {
    max-width: 1200px;
    margin: 0 auto;
    padding: 1.5rem 0;
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}
</style>
