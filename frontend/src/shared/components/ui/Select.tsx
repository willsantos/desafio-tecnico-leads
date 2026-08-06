import { forwardRef, type SelectHTMLAttributes } from 'react'
import { Label } from './Label'
import { Text } from './Text'
import styles from './Select.module.css'

interface SelectOption {
  value: string
  label: string
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string
  error?: string
  options: SelectOption[]
  placeholder?: string
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, error, options, placeholder, id, className = '', ...rest }, ref) => {
    const selectId = id ?? rest.name

    return (
      <div className={[styles.wrapper, className].join(' ')}>
        {label && (
          <Label htmlFor={selectId} required={rest.required}>
            {label}
          </Label>
        )}
        <select
          ref={ref}
          id={selectId}
          className={[styles.select, error ? styles.selectError : ''].join(' ')}
          aria-invalid={error ? 'true' : 'false'}
          aria-describedby={error ? `${selectId}-error` : undefined}
          {...rest}
        >
          {placeholder && (
            <option value="" disabled>
              {placeholder}
            </option>
          )}
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        {error && (
          <Text
            id={`${selectId}-error`}
            variant="caption"
            as="span"
            className={styles.error}
            aria-live="polite"
          >
            {error}
          </Text>
        )}
      </div>
    )
  }
)

Select.displayName = 'Select'
