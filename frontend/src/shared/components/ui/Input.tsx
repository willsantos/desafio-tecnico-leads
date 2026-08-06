import { forwardRef, useEffect, useState, type InputHTMLAttributes } from 'react'
import { unmaskDigits } from '../../utils/formatters'
import { Label } from './Label'
import { Text } from './Text'
import styles from './Input.module.css'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  /** When provided, the input displays the masked value and calls onValueChange with the raw digits. */
  mask?: (value: string) => string
  onValueChange?: (value: string) => void
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, id, className = '', mask, onValueChange, value, onChange, ...rest }, ref) => {
    const inputId = id ?? rest.name
    const isMasked = Boolean(mask)
    const rawValue = String(value ?? '')
    const [displayValue, setDisplayValue] = useState(isMasked ? mask?.(rawValue) ?? rawValue : rawValue)

    useEffect(() => {
      if (isMasked) {
        setDisplayValue(mask?.(rawValue) ?? rawValue)
      }
    }, [rawValue, isMasked, mask])

    function handleChange(event: React.ChangeEvent<HTMLInputElement>) {
      if (isMasked && onValueChange) {
        const raw = unmaskDigits(event.target.value)
        setDisplayValue(mask?.(raw) ?? raw)
        onValueChange(raw)
      } else {
        onChange?.(event)
      }
    }

    return (
      <div className={[styles.wrapper, className].join(' ')}>
        {label && (
          <Label htmlFor={inputId} required={rest.required}>
            {label}
          </Label>
        )}
        <input
          ref={ref}
          id={inputId}
          className={[styles.input, error ? styles.inputError : ''].join(' ')}
          aria-invalid={error ? 'true' : 'false'}
          aria-describedby={error ? `${inputId}-error` : undefined}
          {...rest}
          value={isMasked ? displayValue : value}
          onChange={handleChange}
        />
        {error && (
          <Text
            id={`${inputId}-error`}
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

Input.displayName = 'Input'
