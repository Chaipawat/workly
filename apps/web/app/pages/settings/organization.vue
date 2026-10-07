<script setup lang="ts">
import { Building2, Save, ShieldCheck, UserRound } from '@lucide/vue'
import { getAuthErrorMessage } from '~/utils/auth'

definePageMeta({ middleware: 'auth' })

const workspace = useWorkspace()
const { currentOrganization } = workspace
const name = ref('')
const slug = ref('')
const loading = ref(true)
const saving = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const auth = useAuth()
const profileName = ref('')
const profileSaving = ref(false)
const profileMessage = ref('')
const profileError = ref('')
const passwordForm = reactive({ currentPassword: '', newPassword: '', confirmPassword: '' })
const passwordSaving = ref(false)
const passwordError = ref('')

const canEdit = computed(() => ['owner', 'admin'].includes(currentOrganization.value?.role ?? ''))

onMounted(async () => {
  try {
    await workspace.initialize()
    name.value = currentOrganization.value?.name ?? ''
    slug.value = currentOrganization.value?.slug ?? ''
    profileName.value = auth.user.value?.displayName ?? ''
  }
  catch (error) {
    errorMessage.value = getAuthErrorMessage(error)
  }
  finally {
    loading.value = false
  }
})

watch(() => currentOrganization.value?.id, () => {
  name.value = currentOrganization.value?.name ?? ''
  slug.value = currentOrganization.value?.slug ?? ''
  errorMessage.value = ''
  successMessage.value = ''
})

const save = async () => {
  if (!currentOrganization.value || !canEdit.value) return
  errorMessage.value = ''
  successMessage.value = ''
  saving.value = true
  try {
    const organization = await workspace.updateOrganization(currentOrganization.value.id, name.value.trim(), slug.value.trim())
    name.value = organization.name
    slug.value = organization.slug
    successMessage.value = 'Workspace settings updated.'
  }
  catch (error) {
    errorMessage.value = getAuthErrorMessage(error)
  }
  finally {
    saving.value = false
  }
}

const changePassword = async () => {
  passwordError.value = ''
  if (passwordForm.newPassword !== passwordForm.confirmPassword) {
    passwordError.value = 'New passwords do not match.'
    return
  }
  passwordSaving.value = true
  try {
    await auth.changePassword(passwordForm.currentPassword, passwordForm.newPassword)
    await navigateTo('/login?passwordChanged=1')
  }
  catch (error) {
    passwordError.value = getAuthErrorMessage(error)
  }
  finally {
    passwordSaving.value = false
  }
}

const saveProfile = async () => {
  profileError.value = ''
  profileMessage.value = ''
  profileSaving.value = true
  try {
    const updatedUser = await auth.updateProfile(profileName.value.trim())
    profileName.value = updatedUser.displayName
    profileMessage.value = 'Profile updated.'
  }
  catch (error) {
    profileError.value = getAuthErrorMessage(error)
  }
  finally {
    profileSaving.value = false
  }
}
</script>

<template>
  <div class="settings-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">Workspace settings</p>
        <h1>Organization profile</h1>
        <p>Manage your workspace identity and account security.</p>
      </div>
    </header>

    <section class="card settings-card" aria-labelledby="workspace-name-heading">
      <div class="settings-card__icon" aria-hidden="true"><Building2 :size="22" /></div>
      <div class="settings-card__content">
        <div>
          <h2 id="workspace-name-heading">Workspace name</h2>
          <p>This is visible to everyone in your organization.</p>
        </div>

        <p v-if="errorMessage" class="form-alert" role="alert">{{ errorMessage }}</p>
        <p v-if="successMessage" class="form-success" role="status">{{ successMessage }}</p>

        <form class="settings-form" @submit.prevent="save">
          <label>
            Name
            <span class="input-control">
              <input v-model="name" type="text" minlength="2" maxlength="160" required :disabled="loading || saving || !canEdit">
            </span>
          </label>
          <label>
            Workspace URL slug
            <span class="input-control">
              <input v-model="slug" type="text" pattern="[a-z0-9]+(?:-[a-z0-9]+)*" minlength="2" maxlength="80" required :disabled="loading || saving || !canEdit">
            </span>
            <small>Lowercase letters, numbers, and hyphens only.</small>
          </label>
          <p v-if="!loading && !canEdit" class="settings-form__hint">Only workspace owners and admins can change this setting.</p>
          <button class="button button--primary" type="submit" :disabled="loading || saving || !canEdit || name.trim().length < 2 || slug.trim().length < 2">
            <Save :size="17" /> {{ saving ? 'Saving…' : 'Save changes' }}
          </button>
        </form>
      </div>
    </section>

    <section class="card settings-card" aria-labelledby="personal-profile-heading">
      <div class="settings-card__icon" aria-hidden="true"><UserRound :size="22" /></div>
      <div class="settings-card__content">
        <div><h2 id="personal-profile-heading">Personal profile</h2><p>Update the name shown to people across your workspaces.</p></div>
        <p v-if="profileError" class="form-alert" role="alert">{{ profileError }}</p>
        <p v-if="profileMessage" class="form-success" role="status">{{ profileMessage }}</p>
        <form class="settings-form" @submit.prevent="saveProfile">
          <label>Display name<span class="input-control"><input v-model="profileName" type="text" autocomplete="name" minlength="2" maxlength="120" required></span></label>
          <label>Email<span class="input-control"><input :value="auth.user.value?.email" type="email" disabled></span><small>Your email is used to sign in and cannot be changed here.</small></label>
          <button class="button button--primary" type="submit" :disabled="profileSaving || profileName.trim().length < 2"><Save :size="17" /> {{ profileSaving ? 'Saving…' : 'Save profile' }}</button>
        </form>
      </div>
    </section>

    <section class="card settings-card" aria-labelledby="account-security-heading">
      <div class="settings-card__icon" aria-hidden="true"><ShieldCheck :size="22" /></div>
      <div class="settings-card__content">
        <div><h2 id="account-security-heading">Account security</h2><p>Change your password. All existing sessions will be signed out.</p></div>
        <p v-if="passwordError" class="form-alert" role="alert">{{ passwordError }}</p>
        <form class="settings-form" @submit.prevent="changePassword">
          <label>Current password<span class="input-control"><input v-model="passwordForm.currentPassword" type="password" autocomplete="current-password" required></span></label>
          <label>New password<span class="input-control"><input v-model="passwordForm.newPassword" type="password" autocomplete="new-password" minlength="6" maxlength="128" required></span><small>At least 6 characters; a longer unique password is safer.</small></label>
          <label>Confirm new password<span class="input-control"><input v-model="passwordForm.confirmPassword" type="password" autocomplete="new-password" minlength="6" maxlength="128" required></span></label>
          <button class="button button--primary" type="submit" :disabled="passwordSaving"><ShieldCheck :size="17" /> {{ passwordSaving ? 'Updating…' : 'Change password' }}</button>
        </form>
      </div>
    </section>
  </div>
</template>
