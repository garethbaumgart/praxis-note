export interface ArchetypeDetail {
  name: string;
  tagline: string;
  description: string;
  strengths: string[];
  weaknesses: string[];
  growthTips: string[];
}

export const ARCHETYPE_DETAILS: Record<string, ArchetypeDetail> = {
  Facilitator: {
    name: 'Facilitator',
    tagline: 'Draws others out, balances airtime',
    description:
      'You create space for all voices, ask thoughtful questions, and ensure balanced participation. Meetings you lead feel inclusive and collaborative.',
    strengths: [
      'Balanced airtime',
      'Thoughtful questions',
      'High engagement',
      'Inclusive approach',
    ],
    weaknesses: [
      'May avoid direct opinions',
      'Can defer decisions too much',
      'May struggle in crisis',
    ],
    growthTips: [
      'Practice asserting your own position',
      'Set clear decision deadlines',
      'Challenge ideas more directly',
    ],
  },
  Driver: {
    name: 'Driver',
    tagline: 'Takes charge, drives outcomes',
    description:
      'You set clear direction, make decisive calls, and keep meetings focused on results. Your presence brings structure and momentum.',
    strengths: [
      'Clear direction',
      'Decisive action',
      'Strong presence',
      'Results-focused',
    ],
    weaknesses: [
      'May interrupt others',
      'Can dominate airtime',
      'Risk missing input',
    ],
    growthTips: [
      "Pause for others' input",
      'Ask more questions',
      'Let silence create space',
    ],
  },
  Observer: {
    name: 'Observer',
    tagline: 'Listens deeply, contributes selectively',
    description:
      'You listen carefully, reflect before speaking, and bring thoughtful insights when you do contribute. Your calm presence steadies meetings.',
    strengths: [
      'Active listening',
      'Thoughtful contributions',
      'Calm presence',
      'Reflective approach',
    ],
    weaknesses: [
      'May be seen as disengaged',
      'Can miss influence opportunities',
      'Risk being overlooked',
    ],
    growthTips: [
      'Speak up earlier',
      'Share thinking-in-progress',
      'Ask clarifying questions',
    ],
  },
  Mediator: {
    name: 'Mediator',
    tagline: 'Bridges perspectives, resolves tension',
    description:
      'You find common ground, maintain a positive tone, and help navigate conflict. Meetings feel psychologically safe and constructive.',
    strengths: [
      'Conflict resolution',
      'Positive tone',
      'Bridge-building',
      'Emotional intelligence',
    ],
    weaknesses: [
      'May avoid necessary conflict',
      'Can be seen as non-committal',
      'Risk smoothing over issues',
    ],
    growthTips: [
      'Name tensions directly',
      'Take clearer positions',
      'Let healthy conflict unfold',
    ],
  },
  Challenger: {
    name: 'Challenger',
    tagline: 'Pushes thinking, asks hard questions',
    description:
      'You probe assumptions, voice disagreement, and push for deeper analysis. Your presence drives better decisions and intellectual rigor.',
    strengths: [
      'Critical thinking',
      'Probing questions',
      'Intellectual rigor',
      'Drives clarity',
    ],
    weaknesses: [
      'Can come across as negative',
      'May derail progress',
      'Risk alienating others',
    ],
    growthTips: [
      'Balance critique with support',
      'Acknowledge good ideas first',
      'Frame challenges constructively',
    ],
  },
  Supporter: {
    name: 'Supporter',
    tagline: 'Encourages, builds on ideas',
    description:
      'You affirm others, build on their ideas, and maintain a warm, collaborative tone. Meetings feel psychologically safe and energizing.',
    strengths: [
      'Encouraging tone',
      'Collaborative spirit',
      'Builds on ideas',
      'Psychological safety',
    ],
    weaknesses: [
      'May avoid disagreement',
      'Can defer too much',
      'Risk not adding critical perspective',
    ],
    growthTips: [
      'Voice concerns directly',
      'Challenge ideas constructively',
      'Own your expertise',
    ],
  },
};
