<script setup lang="ts">
withDefaults(defineProps<{ initials: string, size?: 'sm' | 'md' | 'lg', tone?: string }>(), {
  size: 'md',
  tone: 'blue',
})
</script>

<template>
  <span class="avatar" :class="[`avatar--${size}`, `avatar--${tone}`]" aria-hidden="true">{{ initials }}</span>
</template>
