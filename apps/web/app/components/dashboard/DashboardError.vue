<script setup lang="ts">
import { AlertCircle, RotateCw } from '@lucide/vue'
</script>

<template>
  <section class="card state-card" role="alert">
    <span class="state-card__icon state-card__icon--error">
      <AlertCircle :size="28" />
    </span>
    <h2>We couldn’t load your dashboard</h2>
    <p>Your workspace data is temporarily unavailable. Check your connection and try again.</p>
    <button class="button button--primary" type="button" @click="reloadNuxtApp()">
      <RotateCw :size="17" /> Try again
    </button>
  </section>
</template>
