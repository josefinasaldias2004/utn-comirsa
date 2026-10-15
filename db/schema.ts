import { sqliteTable, text, integer } from 'drizzle-orm/sqlite-core';
export const listings = sqliteTable('listings', {
id:text('id').primaryKey(),owner:text('owner').notNull(),name:text('name').notNull(),
title:text('title').notNull(),description:text('description').notNull(),type:text('type').notNull(),
org:text('org').notNull(),category:text('category').notNull(),price:integer('price'),
contact:text('contact').notNull(),image:text('image'),created:integer('created').notNull()});