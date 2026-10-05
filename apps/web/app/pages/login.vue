<script setup lang="ts">
import { ArrowRight, LockKeyhole, Mail } from '@lucide/vue'
import { getAuthErrorMessage, safeAuthRedirect } from '~/utils/auth'

definePageMeta({ layout: 'auth', middleware: 'guest' })

const route = useRoute()
const auth = useAuth()
const form = reactive({ email: '', password: '' })
const submitting = ref(false)
const errorMessage = ref('')

const submit = async () => {
  errorMessage.value = ''
  submitting.value = true
  try {
    await auth.login(form)
    await navigateTo(safeAuthRedirect(route.query.redirect))
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
      <p class="auth-card__eyebrow">Welcome back</p>
      <h2>Sign in to Workly</h2>
      <p>Use the account you created for your workspace.</p>
    </header>
    <p v-if="errorMessage" class="form-alert" role="alert">{{ errorMessage }}</p>
    <form class="auth-form" @submit.prevent="submit">
      <label>
        <span>Email address</span>
        <span class="input-control"><Mail :size="18" aria-hidden="true" /><input v-model.trim="form.email" type="email" name="email" autocomplete="email" placeholder="you@company.com" maxlength="320" required></span>
      </label>
      <label>
        <span>Password</span>
        <span class="input-control"><LockKeyhole :size="18" aria-hidden="true" /><input v-model="form.password" type="password" name="password" autocomplete="current-password" placeholder="Your password" maxlength="128" required></span>
      </label>
      <button class="button button--primary auth-submit" type="submit" :disabled="submitting">
        {{ submitting ? 'Signing in…' : 'Sign in' }}<ArrowRight v-if="!submitting" :size="18" aria-hidden="true" />
      </button>
    </form>
    <p class="auth-card__switch">New to Workly? <NuxtLink to="/register">Create an account</NuxtLink></p>
  </div>
</template>
