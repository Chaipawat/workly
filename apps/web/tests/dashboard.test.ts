import { describe, expect, it } from 'vitest'
import { getProjectProgress, getTaskTotal } from '~/utils/dashboard'

describe('dashboard calculations', () => {
  it('rounds project progress for display', () => {
    expect(getProjectProgress({ completedTasks: 13, totalTasks: 18 })).toBe(72)
  })

  it('safely handles projects without tasks', () => {
    expect(getProjectProgress({ completedTasks: 0, totalTasks: 0 })).toBe(0)
  })

  it('keeps progress inside valid bounds', () => {
    expect(getProjectProgress({ completedTasks: 8, totalTasks: 4 })).toBe(100)
    expect(getProjectProgress({ completedTasks: -1, totalTasks: 4 })).toBe(0)
  })

  it('totals all task statuses', () => {
    expect(getTaskTotal({ todo: 18, 'in-progress': 12, review: 5, done: 31 })).toBe(66)
  })
})
