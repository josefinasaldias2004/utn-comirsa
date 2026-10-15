CREATE TABLE `listings` (
	`id` text PRIMARY KEY NOT NULL,
	`owner` text NOT NULL,
	`name` text NOT NULL,
	`title` text NOT NULL,
	`description` text NOT NULL,
	`type` text NOT NULL,
	`org` text NOT NULL,
	`category` text NOT NULL,
	`price` integer,
	`contact` text NOT NULL,
	`image` text,
	`created` integer NOT NULL
);
