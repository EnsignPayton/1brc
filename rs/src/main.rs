use itertools::Itertools;
use std::collections::HashMap;
use std::error::Error;
use std::fs::File;
use std::io::{BufRead as _, BufReader};

fn main() -> Result<(), Box<dyn Error>> {
    let input_path = std::env::args().nth(1).ok_or("")?;

    let start = std::time::SystemTime::now();
    let map = parse_input(input_path)?;
    let end = std::time::SystemTime::now();

    let duration = end.duration_since(start)?;

    println!("{}", map);
    println!("Processing took {:.3}s", duration.as_secs_f32());

    Ok(())
}

fn parse_input<P>(input_path: P) -> Result<WeatherMap, Box<dyn Error>>
where
    P: AsRef<std::path::Path>,
{
    let mut map = WeatherMap::new();

    let file = File::open(input_path)?;
    let mut reader = BufReader::new(file);
    let mut line = String::new();

    while reader.read_line(&mut line)? > 0 {
        let i_sep = line.find(';').ok_or("")?;
        let key = line.get(0..i_sep).ok_or("")?;
        let value = parse_temperature(line.get(i_sep + 1..).ok_or("")?.trim()).ok_or("")?;

        map.update(key, value);
        line.clear();
    }

    Ok(map)
}

fn parse_temperature(input: &str) -> Option<i16> {
    const NEG_SIGN: u8 = '-' as u8;
    const DOT: u8 = '.' as u8;
    const ZERO: u8 = '0' as u8;

    let mut bytes = input.bytes();

    let first = bytes.next();

    let negative = match first {
        Some(NEG_SIGN) => true,
        _ => false,
    };

    let sign: i16 = match negative {
        true => -1,
        false => 1,
    };

    let msd = if !negative {
        first? - ZERO
    } else {
        bytes.next()?
    };

    let next = bytes.next();

    // Single digit without decimal
    if next.is_none() {
        return Some(sign * 10 * msd as i16);
    }

    // Single digit with decimal
    if next? == DOT {
        let ones = bytes.next()? - ZERO;
        return Some(sign * (10 * msd as i16 + ones as i16));
    }

    let nsd = next? - ZERO;
    let next = bytes.next();

    // Two digits without decimal
    if next.is_none() {
        return Some(sign * (100 * msd as i16 + 10 * nsd as i16));
    }

    // Two digits with decimal
    let ones = bytes.next()? - ZERO;
    Some(sign * (100 * msd as i16 + 10 * nsd as i16 + ones as i16))
}

struct WeatherData {
    min: i16,
    max: i16,
    total: i64,
    count: u64,
}

impl WeatherData {
    fn new(initial_value: i16) -> WeatherData {
        WeatherData {
            min: initial_value,
            max: initial_value,
            total: initial_value as i64,
            count: 1,
        }
    }

    fn update(&mut self, value: i16) {
        if value < self.min {
            self.min = value
        }
        if value > self.max {
            self.max = value
        }
        self.count += 1;
        self.total += value as i64;
    }
}

impl std::fmt::Display for WeatherData {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        let mean = self.total / self.count as i64;
        write!(
            f,
            "{}.{}/{}.{}/{}.{}",
            self.min / 10,
            self.min.abs() % 10,
            mean / 10,
            mean.abs() % 10,
            self.max / 10,
            self.max.abs() % 10
        )
    }
}

struct WeatherMap {
    data: HashMap<String, WeatherData>,
}

impl WeatherMap {
    fn new() -> WeatherMap {
        WeatherMap {
            data: HashMap::new(),
        }
    }

    fn update(&mut self, key: &str, value: i16) {
        if self.data.contains_key(key) {
            let weather_data: &mut WeatherData = self.data.get_mut(key).unwrap();
            weather_data.update(value);
        } else {
            self.data.insert(key.to_string(), WeatherData::new(value));
        }
    }
}

impl std::fmt::Display for WeatherMap {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{{")?;

        let mut is_first = true;
        for key in self.data.keys().sorted() {
            if !is_first {
                write!(f, ", ")?;
            }

            write!(f, "{}={}", key, &self.data[key])?;

            is_first = false;
        }

        write!(f, "}}")
    }
}
