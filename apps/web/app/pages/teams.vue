<script setup lang="ts">
import { Plus, Save, Trash2 } from '@lucide/vue'
import type { Department, Member } from '~/types/operations'
import { getAuthErrorMessage } from '~/utils/auth'

definePageMeta({ middleware: 'auth' })
const { request } = useOperations(); const departments = ref<Department[]>([]); const people = ref<Member[]>([])
const loading = ref(true); const error = ref(''); const form = reactive({ name: '', managerId: '' })
const managers = computed(() => people.value.filter(person => ['manager', 'hr', 'admin', 'owner'].includes(person.role) && person.status === 'active'))
const load = async () => { loading.value = true; error.value = ''; try { [departments.value, people.value] = await Promise.all([request<Department[]>('departments'), request<Member[]>('people')]) } catch (cause) { error.value = getAuthErrorMessage(cause) } finally { loading.value = false } }
onMounted(load)
const create = async () => { try { await request<Department>('departments', { method: 'POST', body: { name: form.name, managerId: form.managerId || null } }); form.name = ''; form.managerId = ''; await load() } catch (cause) { error.value = getAuthErrorMessage(cause) } }
const save = async (department: Department) => { try { await request<Department>(`departments/${department.id}`, { method: 'PATCH', body: { name: department.name, managerId: department.managerId || null } }); await load() } catch (cause) { error.value = getAuthErrorMessage(cause) } }
const remove = async (department: Department) => { if (!confirm(`Delete ${department.name}?`)) return; try { await request<unknown>(`departments/${department.id}`, { method: 'DELETE' }); await load() } catch (cause) { error.value = getAuthErrorMessage(cause) } }
</script>
<template><div class="resource-page"><header class="page-header"><div><p class="eyebrow">Workspace</p><h1>Teams</h1><p>Organize people into departments and assign managers.</p></div></header><p v-if="error" class="form-alert" role="alert">{{ error }}</p>
  <form class="card resource-form" @submit.prevent="create"><h2>Create department</h2><div class="field-grid"><label>Name<input v-model="form.name" required minlength="2"></label><label>Manager<select v-model="form.managerId"><option value="">No manager</option><option v-for="person in managers" :key="person.id" :value="person.id">{{ person.name }}</option></select></label></div><button class="button button--primary"><Plus :size="17" /> Create team</button></form>
  <div class="resource-grid"><p v-if="loading" class="card resource-state">Loading teams…</p><p v-else-if="departments.length === 0" class="card resource-state">No departments yet.</p><article v-for="department in departments" v-else :key="department.id" class="card resource-card"><input v-model="department.name" aria-label="Department name"><select v-model="department.managerId"><option :value="null">No manager</option><option v-for="person in managers" :key="person.id" :value="person.id">{{ person.name }}</option></select><p>{{ department.memberCount }} members</p><div class="resource-actions"><button class="button button--secondary button--compact" @click="save(department)"><Save :size="15" /> Save</button><button class="button button--danger button--compact" @click="remove(department)"><Trash2 :size="15" /> Delete</button></div></article></div>
</div></template>
