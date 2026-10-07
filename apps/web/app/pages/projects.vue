<script setup lang="ts">
import { Plus, Save } from '@lucide/vue'
import type { Project } from '~/types/operations'
import { getAuthErrorMessage } from '~/utils/auth'
definePageMeta({ middleware: 'auth' })
const { request } = useOperations(); const projects = ref<Project[]>([]); const loading = ref(true); const error = ref('')
const form = reactive({ name: '', description: '', status: 'active', startDate: '', dueDate: '' })
const statuses = ['active', 'on-hold', 'completed', 'archived']
const payload = (project: typeof form | Project) => ({ name: project.name, description: project.description || null, status: project.status, startDate: project.startDate || null, dueDate: project.dueDate || null })
const load = async () => { loading.value = true; try { projects.value = await request<Project[]>('projects') } catch (cause) { error.value = getAuthErrorMessage(cause) } finally { loading.value = false } }
onMounted(load)
const create = async () => { error.value = ''; try { await request<Project>('projects', { method: 'POST', body: payload(form) }); Object.assign(form, { name: '', description: '', status: 'active', startDate: '', dueDate: '' }); await load() } catch (cause) { error.value = getAuthErrorMessage(cause) } }
const save = async (project: Project) => { try { await request<Project>(`projects/${project.id}`, { method: 'PATCH', body: payload(project) }); await load() } catch (cause) { error.value = getAuthErrorMessage(cause) } }
</script>
<template><div class="resource-page"><header class="page-header"><div><p class="eyebrow">Work management</p><h1>Projects</h1><p>Create projects, set timelines, and track progress.</p></div></header><p v-if="error" class="form-alert" role="alert">{{ error }}</p>
  <form class="card resource-form" @submit.prevent="create"><h2>Create project</h2><div class="field-grid field-grid--3"><label>Name<input v-model="form.name" required minlength="2"></label><label>Start date<input v-model="form.startDate" type="date"></label><label>Due date<input v-model="form.dueDate" type="date"></label></div><label>Description<textarea v-model="form.description" rows="3" /></label><button class="button button--primary"><Plus :size="17" /> Create project</button></form>
  <div class="resource-grid"><p v-if="loading" class="card resource-state">Loading projects…</p><p v-else-if="projects.length === 0" class="card resource-state">No projects yet.</p><article v-for="project in projects" v-else :key="project.id" class="card resource-card"><div class="resource-card__header"><input v-model="project.name" aria-label="Project name"><select v-model="project.status"><option v-for="status in statuses" :key="status">{{ status }}</option></select></div><textarea v-model="project.description" rows="2" placeholder="Description"/><div class="field-grid"><label>Start<input v-model="project.startDate" type="date"></label><label>Due<input v-model="project.dueDate" type="date"></label></div><p>{{ project.completedTasks }} / {{ project.totalTasks }} tasks completed</p><div class="resource-actions"><NuxtLink class="button button--secondary button--compact" :to="`/tasks?project=${project.id}`">View tasks</NuxtLink><button class="button button--primary button--compact" @click="save(project)"><Save :size="15" /> Save</button></div></article></div>
</div></template>
