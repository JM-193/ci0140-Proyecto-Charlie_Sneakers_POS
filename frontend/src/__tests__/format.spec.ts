import { describe, expect, it } from 'vitest'
import { formatColones, parseAmount } from '@/utils/format'

describe('parseAmount', () => {
  it.each([
    ['15000', 15000],
    ['15 000', 15000],
    ['₡20000', 20000],
    ['1500,50', 1500.5],
    ['1500.25', 1500.25],
    ['-5', -5],
  ])('parses "%s"', (input, expected) => {
    expect(parseAmount(input)).toBe(expected)
  })

  it.each(['', '   ', 'abc', '12a', '1.2.3'])('returns null for "%s"', (input) => {
    expect(parseAmount(input)).toBeNull()
  })
})

describe('formatColones', () => {
  it('formats colones without calculating anything', () => {
    expect(formatColones(73450)).toMatch(/73.450/)
    expect(formatColones(73450)).toContain('₡')
  })

  it('shows a dash when there is no amount', () => {
    expect(formatColones(null)).toBe('—')
    expect(formatColones(undefined)).toBe('—')
  })
})
