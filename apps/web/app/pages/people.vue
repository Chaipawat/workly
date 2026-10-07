<script setup lang="ts">
import { Save, UserPlus } from '@lucide/vue'
import type { Department, Member, MemberRole } from '~/types/operations'
import { getAuthErrorMessage } from '~/utils/auth'

definePageMeta({ middleware: 'auth' })
const { request } = useOperations()
const people = ref<Member[]>([])
const departments = ref<Department[]>([])
const loading = ref(true)
const busyId = ref('')
const error = ref('')
const form = reactive({ email: '', jobTitle: '', role: 'employee' as MemberRole, departmentId: '' })
const roles: MemberRole[] = ['employee', 'manager', 'hr', 'admin']

const load = async () => {
  loading.value = true; error.value = ''
  try { [people.value, departments.value] = await Promise.all([request<Member[]>('people'), request<Department[]>('departments')]) }
  catch (cause) { error.value = getAuthErrorMessage(cause) }
  finally { loading.value = false }
}
onMounted(load)

const add = async () => {
  error.value = ''
  try {
    await request<Member>('people', { method: 'POST', body: { ...form, departmentId: form.departmentId || null } })
    Object.assign(form, { email: '', jobTitle: '', role: 'employee', departmentId: '' }); await load()
  }
  catch (cause) { error.value = getAuthErrorMessage(cause) }
}
const save = async (member: Member) => {
  busyId.value = member.id; error.value = ''
  try { await request<Member>(`people/${member.id}`, { method: 'PATCH', body: { jobTitle: member.jobTitle, role: member.role, status: member.status, departmentId: member.departmentId || null } }); await load() }
  catch (cause) { error.value = getAuthErrorMessage(cause) }
  finally { busyId.value = '' }
}
</script>

<template>
  <div class="resource-page">
    <header class="page-header"><div><p class="eyebrow">Workspace</p><h1>People</h1><p>Manage members, roles, job titles, and departments.</p></div></header>
    <p v-if="error" class="form-alert" role="alert">{{ error }}</p>
    <form class="card resource-form" @submit.prevent="add">
      <h2>Add an existing Workly user</h2>
      <div class="field-grid field-grid--4">
        <label>Email<input v-model="form.email" type="email" required></label>
        <label>Job title<input v-model="form.jobTitle" required maxlength="120"></label>
        <label>Role<select v-model="form.role"><option v-for="role in roles" :key="role">{{ role }}</option></select></label>
        <label>Department<select v-model="form.departmentId"><option value="">No department</option><option v-for="department in departments" :key="department.id" :value="department.id">{{ department.name }}</option></select></label>
      </div>
      <button class="button button--primary" type="submit"><UserPlus :size="17" /> Add member</button>
    </form>
    <div class="card table-card">
      <p v-if="loading" class="resource-state">Loading people…</p>
      <p v-else-if="people.length === 0" class="resource-state">No members yet.</p>
      <div v-else class="table-scroll"><table class="data-table"><thead><tr><th>Person</th><th>Job title</th><th>Department</th><th>Role</th><th>Status</th><th /></tr></thead><tbody>
        <tr v-for="member in people" :key="member.id"><td><strong>{{ member.name }}</strong><small>{{ member.email }}</small></td><td><input v-model="member.jobTitle" maxlength="120"></td><td><select v-model="member.departmentId"><option :value="null">None</option><option v-for="department in departments" :key="department.id" :value="department.id">{{ department.name }}</option></select></td><td><select v-model="member.role" :disabled="member.role === 'owner'"><option v-if="member.role === 'owner'" value="owner">owner</option><option v-for="role in roles" :key="role">{{ role }}</option></select></td><td><select v-model="member.status" :disabled="member.role === 'owner'"><option value="active">active</option><option value="removed">removed</option></select></td><td><button class="button button--secondary button--compact" :disabled="busyId === member.id" @click="save(member)"><Save :size="15" /> Save</button></td></tr>
      </tbody></table></div>
    </div>
  </div>
</template>
