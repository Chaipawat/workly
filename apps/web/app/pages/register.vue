<script setup lang="ts">
import { ArrowRight, LockKeyhole, Mail, UserRound } from '@lucide/vue'
import { getAuthErrorMessage } from '~/utils/auth'

definePageMeta({ layout: 'auth', middleware: 'guest' })

const auth = useAuth()
const form = reactive({ displayName: '', email: '', password: '' })
const submitting = ref(false)
const errorMessage = ref('')

const submit = async () => {
  errorMessage.value = ''
  submitting.value = true
  try {
    await auth.register(form)
    await navigateTo('/dashboard')
  }
  catch (error) {
    errorMessage.value = getAuthErrorMessage(error)
  }
  finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="auth-card">
    <header class="auth-card__header">
      <p class="auth-card__eyebrow">Start fresh</p>
      <h2>Create your account</h2>
      <p>Your workspace and organization setup come next.</p>
    </header>
    <p v-if="errorMessage" class="form-alert" role="alert">{{ errorMessage }}</p>
    <form class="auth-form" @submit.prevent="submit">
      <label>
        <span>Display name</span>
        <span class="input-control"><UserRound :size="18" aria-hidden="true" /><input v-model.trim="form.displayName" name="displayName" autocomplete="name" placeholder="Your name" maxlength="120" required></span>
      </label>
      <label>
        <span>Email address</span>
        <span class="input-control"><Mail :size="18" aria-hidden="true" /><input v-model.trim="form.email" type="email" name="email" autocomplete="email" placeholder="you@company.com" maxlength="320" required></span>
      </label>
      <label>
        <span>Password</span>
        <span class="input-control"><LockKeyhole :size="18" aria-hidden="true" /><input v-model="form.password" type="password" name="password" autocomplete="new-password" placeholder="At least 6 characters" minlength="6" maxlength="128" required></span>
        <small>Use at least 6 characters. A longer unique password is recommended.</small>
      </label>
      <button class="button button--primary auth-submit" type="submit" :disabled="submitting">
        {{ submitting ? 'Creating account…' : 'Create account' }}<ArrowRight v-if="!submitting" :size="18" aria-hidden="true" />
      </button>
    </form>
    <p class="auth-card__switch">Already have an account? <NuxtLink to="/login">Sign in</NuxtLink></p>
  </div>
</template>
