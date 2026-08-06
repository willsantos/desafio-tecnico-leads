import { forwardRef, type InputHTMLAttributes } from 'react'
import { Label } from './Label'
import { Text } from './Text'
import styles from './Input.module.css'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, id, className = '', ...rest }, ref) => {
    const inputId = id ?? rest.name

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
