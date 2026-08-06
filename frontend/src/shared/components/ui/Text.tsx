import type { ReactNode } from 'react'
import styles from './Text.module.css'

type TextVariant = 'display' | 'title' | 'subtitle' | 'body' | 'muted' | 'label' | 'caption'
type TextElement = 'h1' | 'h2' | 'h3' | 'p' | 'span' | 'label' | 'legend' | 'div'

interface TextProps {
  children: ReactNode
  variant?: TextVariant
  as?: TextElement
  className?: string
  id?: string
}

const variantToElement: Record<TextVariant, TextElement> = {
  display: 'h1',
  title: 'h2',
  subtitle: 'h3',
  body: 'p',
  muted: 'p',
  label: 'span',
  caption: 'span',
}

export function Text({ children, variant = 'body', as, className = '', id }: TextProps) {
  const Element = as ?? variantToElement[variant]
  const classNames = [styles.text, styles[variant], className].join(' ')

  return (
    <Element id={id} className={classNames}>
      {children}
    </Element>
  )
}
