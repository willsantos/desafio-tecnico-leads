import type { LabelHTMLAttributes, ReactNode } from 'react'
import styles from './Label.module.css'

interface LabelProps extends LabelHTMLAttributes<HTMLLabelElement> {
  children: ReactNode
  required?: boolean
}

export function Label({ children, required, className = '', ...rest }: LabelProps) {
  return (
    <label className={[styles.label, className].join(' ')} {...rest}>
      {children}
      {required && <span className={styles.required} aria-hidden="true"> *</span>}
    </label>
  )
}
