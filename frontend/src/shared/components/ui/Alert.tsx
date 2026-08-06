import type { ReactNode } from 'react'
import { Text } from './Text'
import styles from './Alert.module.css'

type AlertVariant = 'info' | 'success' | 'warning' | 'error'

interface AlertProps {
  children: ReactNode
  variant?: AlertVariant
  title?: string
  className?: string
}

const icons: Record<AlertVariant, string> = {
  info: 'ℹ️',
  success: '✅',
  warning: '⚠️',
  error: '❌',
}

export function Alert({ children, variant = 'info', title, className = '' }: AlertProps) {
  const classNames = [styles.alert, styles[variant], className].join(' ')

  return (
    <div className={classNames} role="alert">
      <span className={styles.icon} aria-hidden="true">
        {icons[variant]}
      </span>
      <div className={styles.content}>
        {title && (
          <Text variant="label" as="p" className={styles.title}>
            {title}
          </Text>
        )}
        <Text variant="body" as="div">
          {children}
        </Text>
      </div>
    </div>
  )
}
